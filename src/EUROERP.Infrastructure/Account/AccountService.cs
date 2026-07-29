using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using EUROERP.Application.Account;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Account;

/// <summary>Self-service account operations against the legacy aspnet_Membership tables.</summary>
public sealed class AccountService : IAccountService
{
    /// <summary>Legacy validation from changeEmail.aspx — only checks for "something@something.something".</summary>
    private static readonly Regex EmailRegex = new(@"^.*@.*\..*$", RegexOptions.Compiled);

    private readonly IDbConnection _connection;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IDbConnection connection, ILogger<AccountService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<AccountOperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(currentPassword))
            return AccountOperationResult.Fail("A senha atual é necessária.");

        if (string.IsNullOrEmpty(newPassword))
            return AccountOperationResult.Fail("Digite a nova senha.");

        var membership = await GetMembershipAsync(userId, ct).ConfigureAwait(false);
        if (membership == null)
            return AccountOperationResult.Fail("Usuário não encontrado.");

        if (!ValidatePassword(currentPassword, membership.Password, membership.PasswordFormat, membership.PasswordSalt))
            return AccountOperationResult.Fail("Senha atual incorreta.");

        if (!ValidatePasswordFormat(newPassword))
            return AccountOperationResult.Fail("A nova senha deve ter entre 7 e 10 caracteres e conter letras e números.");

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            return AccountOperationResult.Fail("Utilize uma senha diferente da atual.");

        var salt = GenerateSalt();
        var hashed = EncodePasswordSaltFirst(newPassword, salt);

        const string sql = @"
            UPDATE aspnet_Membership
            SET Password = @Password, PasswordFormat = 1, PasswordSalt = @PasswordSalt, LastPasswordChangedDate = @Now
            WHERE UserId = @UserId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Password = hashed,
            PasswordSalt = salt,
            Now = DateTime.UtcNow,
            UserId = userId
        }, cancellationToken: ct)).ConfigureAwait(false);

        _logger.LogInformation("Senha alterada para UserId {UserId}.", userId);
        return AccountOperationResult.Ok("Sua senha foi alterada com sucesso!");
    }

    public async Task<AccountOperationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken ct = default)
    {
        var email = newEmail?.Trim() ?? string.Empty;
        if (email.Length == 0)
            return AccountOperationResult.Fail("Digite o e-mail.");

        if (!EmailRegex.IsMatch(email))
            return AccountOperationResult.Fail("E-mail inválido!");

        var membership = await GetMembershipAsync(userId, ct).ConfigureAwait(false);
        if (membership == null)
            return AccountOperationResult.Fail("Usuário não encontrado.");

        const string duplicateSql = @"
            SELECT TOP 1 1 FROM aspnet_Membership
            WHERE ApplicationId = @ApplicationId AND UserId <> @UserId AND LoweredEmail = @LoweredEmail";
        var duplicate = await _connection.ExecuteScalarAsync<int?>(new CommandDefinition(duplicateSql, new
        {
            ApplicationId = membership.ApplicationId,
            UserId = userId,
            LoweredEmail = email.ToLowerInvariant()
        }, cancellationToken: ct)).ConfigureAwait(false);
        if (duplicate.HasValue)
            return AccountOperationResult.Fail("Já existe um usuário com este e-mail.");

        const string sql = "UPDATE aspnet_Membership SET Email = @Email, LoweredEmail = LOWER(@Email) WHERE UserId = @UserId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { Email = email, UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);

        _logger.LogInformation("E-mail alterado para UserId {UserId}.", userId);
        return AccountOperationResult.Ok("E-mail alterado com sucesso!");
    }

    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = "SELECT Email FROM aspnet_Membership WHERE UserId = @UserId";
        return await _connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task<MembershipPasswordRow?> GetMembershipAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT ApplicationId, Password, PasswordFormat, PasswordSalt
            FROM aspnet_Membership
            WHERE UserId = @UserId";
        return await _connection.QuerySingleOrDefaultAsync<MembershipPasswordRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private static bool ValidatePasswordFormat(string password)
    {
        if (password.Length < 7 || password.Length > 10) return false;
        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }

    /// <summary>Same rules as AuthService: format 0 = clear text, format 1 = SHA1 with salt in either order.</summary>
    private static bool ValidatePassword(string plainPassword, string storedPassword, int passwordFormat, string passwordSalt)
    {
        return passwordFormat switch
        {
            0 => string.Equals(plainPassword, storedPassword, StringComparison.Ordinal),
            1 => string.Equals(EncodePasswordSaltFirst(plainPassword, passwordSalt), storedPassword, StringComparison.Ordinal)
                 || string.Equals(EncodePasswordPasswordFirst(plainPassword, passwordSalt), storedPassword, StringComparison.Ordinal),
            _ => false
        };
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string EncodePasswordSaltFirst(string pass, string saltBase64)
    {
        var saltBytes = Convert.FromBase64String(saltBase64);
        var passwordBytes = Encoding.Unicode.GetBytes(pass);
        var combined = new byte[saltBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);
        return Convert.ToBase64String(SHA1.HashData(combined));
    }

    private static string EncodePasswordPasswordFirst(string pass, string saltBase64)
    {
        var passwordBytes = Encoding.Unicode.GetBytes(pass);
        var saltBytes = Convert.FromBase64String(saltBase64);
        var combined = new byte[passwordBytes.Length + saltBytes.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(saltBytes, 0, combined, passwordBytes.Length, saltBytes.Length);
        return Convert.ToBase64String(SHA1.HashData(combined));
    }

    private sealed class MembershipPasswordRow
    {
        public Guid ApplicationId { get; set; }
        public string Password { get; set; } = string.Empty;
        public int PasswordFormat { get; set; }
        public string PasswordSalt { get; set; } = string.Empty;
    }
}
