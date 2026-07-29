namespace EUROERP.Application.Activities;

public sealed class ActivityOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ActivityOperationResult Ok(string message) => new() { Success = true, Message = message };
    public static ActivityOperationResult Fail(string message) => new() { Success = false, Message = message };
}
