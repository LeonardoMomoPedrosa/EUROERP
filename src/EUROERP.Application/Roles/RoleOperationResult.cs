namespace EUROERP.Application.Roles;

public sealed class RoleOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static RoleOperationResult Ok(string message) => new() { Success = true, Message = message };
    public static RoleOperationResult Fail(string message) => new() { Success = false, Message = message };
}
