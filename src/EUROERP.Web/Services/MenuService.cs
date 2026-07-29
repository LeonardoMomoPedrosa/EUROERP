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

    public IReadOnlyList<(string Label, string Route)> GetLinkableMenuItems()
    {
        var config = GetMenuConfig();
        var result = new List<(string Label, string Route)>();
        foreach (var top in config.TopMenuItems)
            CollectLinkable(top.Children, result, top.Label);
        return result;
    }

    private static void CollectLinkable(List<MenuItemDto> items, List<(string Label, string Route)> result, string parentPath)
    {
        foreach (var item in items)
        {
            var label = item.Label ?? string.Empty;
            var path = string.IsNullOrEmpty(parentPath) ? label : $"{parentPath} » {label}";
            if (!string.IsNullOrWhiteSpace(item.Route))
                result.Add((path, item.Route!.Trim()));
            if (item.Children.Count > 0)
                CollectLinkable(item.Children, result, path);
        }
    }

    public string? ResolveTopItemIdForPath(string path)
    {
        var normalized = NormalizePath(path);
        if (string.IsNullOrEmpty(normalized) || normalized == "/")
            return null;

        var config = GetMenuConfig();
        string? bestTopId = null;
        var bestLen = -1;

        foreach (var top in config.TopMenuItems)
        {
            foreach (var route in EnumerateRoutes(top.Children))
            {
                var r = NormalizePath(route);
                if (string.IsNullOrEmpty(r))
                    continue;
                if (normalized == r || normalized.StartsWith(r + "/", StringComparison.OrdinalIgnoreCase))
                {
                    if (r.Length > bestLen)
                    {
                        bestLen = r.Length;
                        bestTopId = top.Id;
                    }
                }
            }
        }

        return bestTopId;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";
        var p = path.Trim();
        var q = p.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
            p = p[..q];
        if (!p.StartsWith('/'))
            p = "/" + p;
        while (p.Length > 1 && p.EndsWith('/'))
            p = p[..^1];
        return p;
    }

    private static IEnumerable<string> EnumerateRoutes(IEnumerable<MenuItemDto> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Route))
                yield return item.Route!;
            foreach (var child in EnumerateRoutes(item.Children))
                yield return child;
        }
    }
}
