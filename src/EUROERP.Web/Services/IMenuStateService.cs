namespace EUROERP.Web.Services;

public interface IMenuStateService
{
    string? CurrentTopItemId { get; }
    void SetCurrentTopItemId(string? id);
    event Action? OnChange;
}
