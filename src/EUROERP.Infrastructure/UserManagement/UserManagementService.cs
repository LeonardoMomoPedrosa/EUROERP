using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using EUROERP.Application.UserManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.UserManagement;

public sealed class UserManagementService : IUserManagementService
{
    /// <summary>Every new user starts on the default (national) market.</summary>
    private const byte DefaultMarketId = 1;

    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IDbConnection connection, IConfiguration configuration, ILogger<UserManagementService> logger)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListDto>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return Array.Empty<UserListDto>();

        const string sql = @"
            SELECT u.UserId, u.UserName, m.Email, m.CreateDate, m.LastLoginDate
            FROM aspnet_Users u
            INNER JOIN aspnet_Membership m ON m.UserId = u.UserId
            WHERE u.ApplicationId = @ApplicationId
            ORDER BY u.UserName";
        var rows = await _connection.QueryAsync<UserListDto>(
            new CommandDefinition(sql, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<UserManagementResult> CreateUserAsync(string userName, string? email, string password, CancellationToken cancellationToken = default)
    {
        var trimmed = userName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return UserManagementResult.Fail("Nome de usuário é obrigatório.");

        if (string.IsNullOrEmpty(password))
            return UserManagementResult.Fail("Senha é obrigatória.");

        if (!ValidatePasswordFormat(password))
            return UserManagementResult.Fail("Senha deve ter entre 7 e 10 caracteres e conter letras e números.");

        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return UserManagementResult.Fail("Aplicação não configurada. Verifique Authentication:ApplicationName.");

        var lowered = trimmed.ToLowerInvariant();
        var exists = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM aspnet_Users WHERE ApplicationId = @ApplicationId AND LoweredUserName = @LoweredUserName",
            new { ApplicationId = appId.Value, LoweredUserName = lowered },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists != 0)
            return UserManagementResult.Fail("Já existe um usuário com este nome.");

        var userId = Guid.NewGuid();
        var salt = GenerateSalt();
        var hashedPassword = EncodePasswordSaltFirst(password, salt);
        var now = DateTime.UtcNow;

        const string insertUser = @"
            INSERT INTO aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
            VALUES (@ApplicationId, @UserId, @UserName, @LoweredUserName, NULL, 0, @LastActivityDate)";
        await _connection.ExecuteAsync(new CommandDefinition(insertUser, new
        {
            ApplicationId = appId.Value,
            UserId = userId,
            UserName = trimmed,
            LoweredUserName = lowered,
            LastActivityDate = now
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string insertMembership = @"
            INSERT INTO aspnet_Membership (ApplicationId, UserId, Password, PasswordFormat, PasswordSalt, Email, LoweredEmail,
                IsApproved, IsLockedOut, CreateDate, LastLoginDate, LastPasswordChangedDate, LastLockoutDate,
                FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart)
            VALUES (@ApplicationId, @UserId, @Password, 1, @PasswordSalt, @Email, @LoweredEmail,
                1, 0, @CreateDate, @CreateDate, @CreateDate, @CreateDate,
                0, @CreateDate, 0, @CreateDate)";
        var lowerEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        await _connection.ExecuteAsync(new CommandDefinition(insertMembership, new
        {
            ApplicationId = appId.Value,
            UserId = userId,
            Password = hashedPassword,
            PasswordSalt = salt,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            LoweredEmail = lowerEmail,
            CreateDate = now
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        await _connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO MARKET_USER (USER_ID, MARKET_ID) VALUES (@UserId, @MarketId)",
            new { UserId = userId, MarketId = DefaultMarketId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Usuário criado: {UserName}, UserId: {UserId}.", trimmed, userId);
        return UserManagementResult.Ok("Usuário criado com sucesso!");
    }

    public async Task<UserManagementResult> DeleteUserAsync(Guid userId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        if (currentUserName != null)
        {
            var currentId = await GetUserIdByUserNameAsync(currentUserName.Trim(), cancellationToken).ConfigureAwait(false);
            if (currentId.HasValue && currentId.Value == userId)
                return UserManagementResult.Fail("Não é possível excluir o próprio usuário.");
        }

        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return UserManagementResult.Fail("Aplicação não configurada.");

        var exists = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM aspnet_Users WHERE UserId = @UserId AND ApplicationId = @ApplicationId",
            new { UserId = userId, ApplicationId = appId.Value },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists == 0)
            return UserManagementResult.Fail("Usuário não encontrado.");

        await ExecuteAsync("DELETE FROM MARKET_USER WHERE USER_ID = @UserId", userId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("DELETE FROM aspnet_UsersInRoles WHERE UserId = @UserId", userId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("DELETE FROM aspnet_Membership WHERE UserId = @UserId", userId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("IF OBJECT_ID('dbo.SEC_ACTV_RULES','U') IS NOT NULL DELETE FROM SEC_ACTV_RULES WHERE UserId = @UserId", userId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("IF OBJECT_ID('dbo.aspnet_Profile','U') IS NOT NULL DELETE FROM aspnet_Profile WHERE UserId = @UserId", userId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("DELETE FROM aspnet_Users WHERE UserId = @UserId", userId, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Usuário excluído: UserId {UserId}.", userId);
        return UserManagementResult.Ok("Usuário excluído.");
    }

    private Task ExecuteAsync(string sql, Guid userId, CancellationToken ct) =>
        _connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task<Guid?> GetUserIdByUserNameAsync(string userName, CancellationToken ct)
    {
        var appId = await GetApplicationIdAsync(ct).ConfigureAwait(false);
        if (appId == null) return null;
        const string sql = "SELECT UserId FROM aspnet_Users WHERE ApplicationId = @ApplicationId AND LoweredUserName = LOWER(@UserName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationId = appId.Value, UserName = userName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private static bool ValidatePasswordFormat(string password)
    {
        if (password.Length < 7 || password.Length > 10) return false;
        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Salt + password (ASP.NET Membership format), compatible with AuthService.</summary>
    private static string EncodePasswordSaltFirst(string pass, string saltBase64)
    {
        byte[] saltBytes = Convert.FromBase64String(saltBase64);
        byte[] passwordBytes = Encoding.Unicode.GetBytes(pass);
        byte[] combined = new byte[saltBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);
        byte[] hash = SHA1.HashData(combined);
        return Convert.ToBase64String(hash);
    }
}
