namespace EUROERP.Application.UserManagement;

/// <summary>User row for list (Epic 17).</summary>
public sealed class UserListDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public DateTime? CreateDate { get; init; }
    public DateTime? LastLoginDate { get; init; }
}
