using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using EUROERP.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IDbConnection connection, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        _logger = logger;
    }

    public async Task<LoginResult> ValidateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return LoginResult.Fail("Usuário e senha são obrigatórios.");

        var loweredUserName = userName.Trim().ToLowerInvariant();
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
        {
            _logger.LogWarning("Login failed: application not found. ApplicationName: {ApplicationName}", _applicationName);
            return LoginResult.Fail("Usuário ou senha inválidos.");
        }

        var user = await GetMembershipUserAsync(appId.Value, loweredUserName, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return LoginResult.Fail("Usuário ou senha inválidos.");

        if (!user.IsApproved)
            return LoginResult.Fail("Conta não aprovada. Entre em contato com o administrador.");

        if (user.IsLockedOut)
            return LoginResult.Fail("Conta bloqueada. Entre em contato com o administrador.");

        if (!ValidatePassword(password, user.Password, user.PasswordFormat, user.PasswordSalt))
            return LoginResult.Fail("Usuário ou senha inválidos.");

        return LoginResult.Ok(user.UserId, user.UserName);
    }

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task<MembershipUserRow?> GetMembershipUserAsync(Guid applicationId, string loweredUserName, CancellationToken ct)
    {
        const string sql = @"
            SELECT u.UserId, u.UserName, m.Password, m.PasswordFormat, m.PasswordSalt, m.IsApproved, m.IsLockedOut
            FROM aspnet_Users u
            INNER JOIN aspnet_Membership m ON m.UserId = u.UserId
            WHERE u.ApplicationId = @ApplicationId AND u.LoweredUserName = @LoweredUserName";
        return await _connection.QuerySingleOrDefaultAsync<MembershipUserRow>(
            new CommandDefinition(sql, new { ApplicationId = applicationId, LoweredUserName = loweredUserName }, cancellationToken: ct)).ConfigureAwait(false);
    }

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
}
