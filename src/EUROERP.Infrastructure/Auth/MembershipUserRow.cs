namespace EUROERP.Infrastructure.Auth;

internal sealed class MembershipUserRow
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int PasswordFormat { get; init; }
    public string PasswordSalt { get; init; } = string.Empty;
    public bool IsApproved { get; init; }
    public bool IsLockedOut { get; init; }
}
