namespace EUROERP.Application.Account;

/// <summary>Result of a self-service account operation (change password / change e-mail).</summary>
public sealed class AccountOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AccountOperationResult Ok(string message) => new() { Success = true, Message = message };
    public static AccountOperationResult Fail(string message) => new() { Success = false, Message = message };
}
