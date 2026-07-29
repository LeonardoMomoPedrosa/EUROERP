using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace EUROERP.Web.Services;

/// <summary>
/// Runs legacy XSLT files (wwwroot/resource_files) against GROUP_REPORT / DATA XML.
/// </summary>
public class SalesReportTransformService
{
    private readonly IWebHostEnvironment _env;

    public SalesReportTransformService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string Transform(string xmlContent, string xsltFileName, IReadOnlyDictionary<string, string>? arguments = null)
    {
        var xsltPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "resource_files", xsltFileName);
        if (!File.Exists(xsltPath))
            return $"<!-- XSLT not found: {xsltFileName} -->";

        var xsltText = File.ReadAllText(xsltPath);
        // .NET cannot map hyphenated extension methods; rewrite to NodeSet
        xsltText = xsltText.Replace("exsl:node-set(", "exsl:NodeSet(", StringComparison.Ordinal);

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        var xsl = new XslCompiledTransform();
        using (var reader = XmlReader.Create(new StringReader(xsltText)))
        {
            xsl.Load(reader, new XsltSettings(enableDocumentFunction: true, enableScript: false), new XmlUrlResolver());
        }

        var xsltArgs = new XsltArgumentList();
        if (arguments != null)
        {
            foreach (var kv in arguments)
                xsltArgs.AddParam(kv.Key, "", kv.Value);
        }

        xsltArgs.AddExtensionObject("http://exslt.org/common", new ExsltCommonExtension());

        using var writer = new StringWriter();
        xsl.Transform(xmlDoc, xsltArgs, writer);
        return writer.ToString();
    }

    /// <summary>
    /// EXSLT common:node-set for result-tree-fragments used by legacy ABC XSL.
    /// Single object overload avoids ambiguous binding in XslCompiledTransform.
    /// </summary>
    public sealed class ExsltCommonExtension
    {
        public XPathNodeIterator NodeSet(object? value)
        {
            if (value is XPathNodeIterator iter)
                return iter;

            if (value is XPathNavigator nav)
            {
                if (nav.NodeType == XPathNodeType.Root)
                    return nav.SelectChildren(XPathNodeType.All);
                return nav.Select(".");
            }

            if (value is IXPathNavigable navigable)
            {
                var n = navigable.CreateNavigator();
                if (n == null)
                    return Empty();
                if (n.NodeType == XPathNodeType.Root)
                    return n.SelectChildren(XPathNodeType.All);
                return n.Select(".");
            }

            return Empty();
        }

        private static XPathNodeIterator Empty()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r/>");
            return doc.CreateNavigator()!.Select("/r[false()]");
        }
    }
}
