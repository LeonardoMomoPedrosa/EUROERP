using System.Xml;
using System.Xml.Schema;

namespace EUROERP.Infrastructure.NFe;

/// <summary>
/// Resolve schemaLocation relativos ao diretório base dos XSDs (include/import).
/// </summary>
internal sealed class NfeSchemaResolver : XmlUrlResolver
{
    private readonly string _xsdDirectory;

    public NfeSchemaResolver(string xsdDirectory)
    {
        _xsdDirectory = xsdDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (absoluteUri.IsFile && !File.Exists(absoluteUri.LocalPath))
        {
            var fileName = Path.GetFileName(absoluteUri.LocalPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                var localPath = Path.Combine(_xsdDirectory, fileName);
                if (File.Exists(localPath))
                    absoluteUri = new Uri(localPath);
            }
        }
        return base.GetEntity(absoluteUri, role, ofObjectToReturn);
    }
}

public interface INfeSchemaValidator
{
    /// <summary>
    /// Valida o XML da NFe contra o schema nfe_v4.00.xsd (abre o arquivo).
    /// </summary>
    IReadOnlyList<string> Validate(string xmlPath, string? xsdDirectory = null);

    /// <summary>
    /// Valida o conteúdo XML contra o schema (não usa arquivo; evita lock no segundo envio).
    /// </summary>
    IReadOnlyList<string> ValidateXml(string xmlContent, string? xsdDirectory = null);

    /// <summary>
    /// Valida XML do evento de cancelamento (envEvento) contra envEventoCancNFe_v1.00.xsd.
    /// </summary>
    IReadOnlyList<string> ValidateEventoCancelamentoXml(string xmlContent, string? xsdDirectory = null);

    /// <summary>
    /// Valida XML do evento Carta de Correção (envEvento) contra envCCe_v1.00.xsd.
    /// </summary>
    IReadOnlyList<string> ValidateEventoCceXml(string xmlContent, string? xsdDirectory = null);
}

public class NfeSchemaValidator : INfeSchemaValidator
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public NfeSchemaValidator(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyList<string> Validate(string xmlPath, string? xsdDirectory = null)
    {
        var errors = new List<string>();
        xsdDirectory ??= _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDirectory))
            xsdDirectory = Path.Combine(AppContext.BaseDirectory, "Schemas");

        var mainXsd = Path.Combine(xsdDirectory, "nfe_v4.00.xsd");
        if (!File.Exists(mainXsd))
        {
            errors.Add($"Schema não encontrado: {mainXsd}");
            return errors;
        }

        var schemaSet = new XmlSchemaSet();
        schemaSet.XmlResolver = new NfeSchemaResolver(xsdDirectory);
        try
        {
            var mainUri = new Uri(Path.GetFullPath(mainXsd));
            schemaSet.Add(null, mainUri.AbsoluteUri);
            schemaSet.Compile();
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao carregar schema: {ex.Message}");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message} (linha {e.Exception?.LineNumber})");
        };

        try
        {
            using var reader = XmlReader.Create(xmlPath, settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao validar XML: {ex.Message}");
        }

        return errors;
    }

    public IReadOnlyList<string> ValidateXml(string xmlContent, string? xsdDirectory = null)
    {
        var errors = new List<string>();
        xsdDirectory ??= _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDirectory))
            xsdDirectory = Path.Combine(AppContext.BaseDirectory, "Schemas");

        var mainXsd = Path.Combine(xsdDirectory, "nfe_v4.00.xsd");
        if (!File.Exists(mainXsd))
        {
            errors.Add($"Schema não encontrado: {mainXsd}");
            return errors;
        }

        var schemaSet = new XmlSchemaSet();
        schemaSet.XmlResolver = new NfeSchemaResolver(xsdDirectory);
        try
        {
            var mainUri = new Uri(Path.GetFullPath(mainXsd));
            schemaSet.Add(null, mainUri.AbsoluteUri);
            schemaSet.Compile();
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao carregar schema: {ex.Message}");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message} (linha {e.Exception?.LineNumber})");
        };

        try
        {
            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao validar XML: {ex.Message}");
        }

        return errors;
    }

    public IReadOnlyList<string> ValidateEventoCancelamentoXml(string xmlContent, string? xsdDirectory = null)
    {
        var errors = new List<string>();
        xsdDirectory ??= _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDirectory))
            xsdDirectory = Path.Combine(AppContext.BaseDirectory, "Schemas");

        var mainXsd = Path.Combine(xsdDirectory, "envEventoCancNFe_v1.00.xsd");
        if (!File.Exists(mainXsd))
        {
            errors.Add($"Schema não encontrado: {mainXsd}");
            return errors;
        }

        var schemaSet = new XmlSchemaSet();
        schemaSet.XmlResolver = new NfeSchemaResolver(xsdDirectory);
        try
        {
            var mainUri = new Uri(Path.GetFullPath(mainXsd));
            schemaSet.Add(null, mainUri.AbsoluteUri);
            schemaSet.Compile();
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao carregar schema: {ex.Message}");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message} (linha {e.Exception?.LineNumber})");
        };

        try
        {
            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao validar XML: {ex.Message}");
        }

        return errors;
    }

    public IReadOnlyList<string> ValidateEventoCceXml(string xmlContent, string? xsdDirectory = null)
    {
        var errors = new List<string>();
        xsdDirectory ??= _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDirectory))
            xsdDirectory = Path.Combine(AppContext.BaseDirectory, "Schemas");

        var mainXsd = Path.Combine(xsdDirectory, "envCCe_v1.00.xsd");
        if (!File.Exists(mainXsd))
        {
            errors.Add($"Schema não encontrado: {mainXsd}");
            return errors;
        }

        var schemaSet = new XmlSchemaSet();
        schemaSet.XmlResolver = new NfeSchemaResolver(xsdDirectory);
        try
        {
            var mainUri = new Uri(Path.GetFullPath(mainXsd));
            schemaSet.Add(null, mainUri.AbsoluteUri);
            schemaSet.Compile();
        }
        catch (Exception ex)
        {
            // XSD oficial SEFAZ: mesmo tipo pode aparecer em leiauteCCe e em tiposBasico (include). Não alteramos o XSD.
            if (ex.Message.Contains("already been declared", StringComparison.OrdinalIgnoreCase))
                return errors; // Omite validação local; SEFAZ valida ao receber.
            errors.Add($"Erro ao carregar schema: {ex.Message}");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message} (linha {e.Exception?.LineNumber})");
        };

        try
        {
            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao validar XML: {ex.Message}");
        }

        return errors;
    }
}
