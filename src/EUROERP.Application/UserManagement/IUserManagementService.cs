namespace EUROERP.Application.UserManagement;

/// <summary>User management: list, create, delete (Epic 17).</summary>
public interface IUserManagementService
{
    Task<IReadOnlyList<UserListDto>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<UserManagementResult> CreateUserAsync(string userName, string? email, string password, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user. Fails if userId is the current user.</summary>
    Task<UserManagementResult> DeleteUserAsync(Guid userId, string? currentUserName, CancellationToken cancellationToken = default);
}
