namespace EUROERP.Application.Auth;

public interface IAuthService
{
    Task<LoginResult> ValidateAsync(string userName, string password, CancellationToken cancellationToken = default);
}
