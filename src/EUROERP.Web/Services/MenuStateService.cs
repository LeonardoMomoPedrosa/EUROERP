namespace EUROERP.Web.Services;

public class MenuStateService : IMenuStateService
{
    public string? CurrentTopItemId { get; private set; }

    public void SetCurrentTopItemId(string? id)
    {
        if (CurrentTopItemId == id)
            return;
        CurrentTopItemId = id;
        OnChange?.Invoke();
    }

    public event Action? OnChange;
}
