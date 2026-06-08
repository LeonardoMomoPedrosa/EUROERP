namespace EUROERP.Application.Auth;

public class LoginResult
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? ErrorMessage { get; init; }

    public static LoginResult Ok(Guid userId, string userName) => new()
    {
        Success = true,
        UserId = userId,
        UserName = userName
    };

    public static LoginResult Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
