namespace EUROERP.Infrastructure.NFe;

public interface INfeFileStorage
{
    string GetOrderDirectory(int orderId);
    /// <summary>Directory for a folder by name (e.g. orderId or "IN" + receiptNo for receipt-in).</summary>
    string GetFolderDirectory(string folderName);
    Task SaveXmlAsync(int orderId, string fileName, string xmlContent, CancellationToken cancellationToken = default);
    Task SaveXmlToFolderAsync(string folderName, string fileName, string xmlContent, CancellationToken cancellationToken = default);
}

public class NfeFileStorage : INfeFileStorage
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public NfeFileStorage(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetBasePath()
    {
        var path = _configuration["NFe:NfeXmlPath"];
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("NFe:NfeXmlPath não configurado.");
        return path;
    }

    public string GetOrderDirectory(int orderId)
    {
        var basePath = GetBasePath();
        return Path.Combine(basePath, orderId.ToString());
    }

    public string GetFolderDirectory(string folderName)
    {
        var basePath = GetBasePath();
        return Path.Combine(basePath, folderName);
    }

    public async Task SaveXmlAsync(int orderId, string fileName, string xmlContent, CancellationToken cancellationToken = default)
    {
        var dir = GetOrderDirectory(orderId);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(fullPath, xmlContent, System.Text.Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveXmlToFolderAsync(string folderName, string fileName, string xmlContent, CancellationToken cancellationToken = default)
    {
        var dir = GetFolderDirectory(folderName);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(fullPath, xmlContent, System.Text.Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }
}
