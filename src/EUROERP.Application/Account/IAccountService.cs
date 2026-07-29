namespace EUROERP.Application.Account;

/// <summary>Self-service account operations for the logged-in user (Epic 18 Story 18.1).</summary>
public interface IAccountService
{
    /// <summary>Changes the user's password after validating the current one. New password is re-hashed with PasswordFormat 1.</summary>
    Task<AccountOperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Changes the user's e-mail, keeping LoweredEmail in sync and enforcing uniqueness within the application.</summary>
    Task<AccountOperationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken ct = default);

    /// <summary>Returns the user's current e-mail, or null when not set / user not found.</summary>
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default);
}
