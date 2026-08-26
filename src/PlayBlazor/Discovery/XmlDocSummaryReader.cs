using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PlayBlazor.Discovery;

/// <summary>Reads member summaries from a compiler-generated XML documentation file.</summary>
public sealed partial class XmlDocSummaryReader
{
    private readonly Dictionary<string, string> _summaries;

    private XmlDocSummaryReader(Dictionary<string, string> summaries)
        => _summaries = summaries;

    public static XmlDocSummaryReader FromStream(Stream stream)
    {
        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        var document = XDocument.Load(stream);
        foreach (var member in document.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary");
            if (name is null || summary is null)
            {
                continue;
            }

            var text = FlattenSummary(summary);
            if (text.Length > 0)
            {
                summaries[name] = text;
            }
        }

        return new XmlDocSummaryReader(summaries);
    }

    public string? GetTypeSummary(Type type)
        => _summaries.GetValueOrDefault($"T:{XmlId(type)}");

    public string? GetPropertySummary(PropertyInfo property)
        => property.DeclaringType is { } declaringType
            ? _summaries.GetValueOrDefault($"P:{XmlId(declaringType)}.{property.Name}")
            : null;

    private static string XmlId(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return (definition.FullName ?? definition.Name).Replace('+', '.');
    }

    private static string FlattenSummary(XElement summary)
    {
        var builder = new StringBuilder();
        foreach (var node in summary.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement { Name.LocalName: "see" } see:
                    builder.Append(SeeText(see));
                    break;
                case XElement element:
                    builder.Append(element.Value);
                    break;
            }
        }

        return WhitespaceRun().Replace(builder.ToString(), " ").Trim();
    }

    private static string SeeText(XElement see)
    {
        if (!string.IsNullOrEmpty(see.Value))
        {
            return see.Value;
        }

        var cref = see.Attribute("cref")?.Value ?? see.Attribute("href")?.Value ?? string.Empty;
        var lastDot = cref.LastIndexOf('.');
        return lastDot < 0 ? cref : cref[(lastDot + 1)..];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
