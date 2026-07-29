namespace EUROERP.Application.UserManagement;

/// <summary>Result of create or delete user operation (Epic 17).</summary>
public sealed class UserManagementResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static UserManagementResult Ok(string message) => new() { Success = true, Message = message };
    public static UserManagementResult Fail(string message) => new() { Success = false, Message = message };
}
