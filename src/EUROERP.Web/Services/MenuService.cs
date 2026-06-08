using System.Text.Json;
using EUROERP.Web.Models.Menu;

namespace EUROERP.Web.Services;

public class MenuService : IMenuService
{
    private readonly string _configPath;
    private MenuConfigDto? _cached;
    private DateTime _cachedFileTime = DateTime.MinValue;

    public MenuService(IWebHostEnvironment env)
    {
        _configPath = Path.Combine(env.WebRootPath ?? "wwwroot", "config", "menu.json");
    }

    public MenuConfigDto GetMenuConfig()
    {
        var fileInfo = new FileInfo(_configPath);
        if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > _cachedFileTime)
            _cached = null;

        if (_cached is not null)
            return _cached;

        if (!File.Exists(_configPath))
        {
            _cached = new MenuConfigDto();
            _cachedFileTime = DateTime.UtcNow;
            return _cached;
        }

        var json = File.ReadAllText(_configPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _cached = JsonSerializer.Deserialize<MenuConfigDto>(json, options) ?? new MenuConfigDto();
        _cachedFileTime = fileInfo.LastWriteTimeUtc;
        return _cached;
    }
}
