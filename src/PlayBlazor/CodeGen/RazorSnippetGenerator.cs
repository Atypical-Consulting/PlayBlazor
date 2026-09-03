using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.CodeGen;

/// <summary>
/// Generates the Razor snippet matching the current playground state — as plain text for
/// the clipboard, and as natively highlighted markup for display. Both views render the
/// same token stream, so they can never drift apart. With options, the snippet also shows
/// what the host contributes to the render: generic closings become type attributes,
/// parameter presets appear as literals (or <c>@_camelCase</c> placeholders when the value
/// has no literal form), and slot presets appear as child elements.
/// </summary>
public static class RazorSnippetGenerator
{
    private const string HostContentComment = "@* … *@";

    private enum TokenKind
    {
        Punctuation,
        Tag,
        AttributeName,
        AttributeValue,
        ChildContent,
        Comment,
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    /// <summary>The snippet as plain text, for the clipboard.</summary>
    /// <param name="component">The played component's descriptor.</param>
    /// <param name="state">The current values; only non-default parameters are emitted.</param>
    /// <param name="options">Host presets, scaffolds and slot sources. Omit them for the bare component.</param>
    public static string Generate(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options = null)
        => string.Concat(Compose(component, state, options).Select(static t => t.Text));

    /// <summary>The same snippet as syntax-highlighted markup, for display.</summary>
    /// <param name="component">The played component's descriptor.</param>
    /// <param name="state">The current values; only non-default parameters are emitted.</param>
    /// <param name="options">Host presets, scaffolds and slot sources. Omit them for the bare component.</param>
    /// <returns>Markup whose text content is identical to <see cref="Generate" />'s output.</returns>
    public static MarkupString GenerateMarkup(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options = null)
    {
        var builder = new StringBuilder();
        foreach (var token in Compose(component, state, options))
        {
            var encoded = WebUtility.HtmlEncode(token.Text);
            if (token.Kind == TokenKind.Punctuation)
            {
                builder.Append(encoded);
            }
            else
            {
                builder.Append("<span class=\"pb-tok-").Append(ClassSuffix(token.Kind)).Append("\">")
                    .Append(encoded).Append("</span>");
            }
        }

        return new MarkupString(builder.ToString());
    }

    private static string ClassSuffix(TokenKind kind)
        => kind switch
        {
            TokenKind.Tag => "tag",
            TokenKind.AttributeName => "attr",
            TokenKind.AttributeValue => "val",
            TokenKind.ChildContent => "content",
            TokenKind.Comment => "comment",
            _ => "punct",
        };

    /// <summary>
    /// The component snippet, wrapped in the host scaffold's razor when the host provided
    /// its source — a copied scaffolded bench must include the parent it needs to run.
    /// </summary>
    private static List<Token> Compose(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options)
    {
        var inner = Tokenize(component, state, options);
        if (options is null || !options.TryGetScaffoldSource(component.Type, out var template))
        {
            return inner;
        }

        var lines = Dedent(template);
        var markerIndex = Array.FindIndex(lines, static l => l.Contains("{specimen}", StringComparison.Ordinal));
        if (markerIndex < 0)
        {
            return inner;
        }

        var indent = lines[markerIndex][..(lines[markerIndex].Length - lines[markerIndex].TrimStart().Length)];
        var tokens = new List<Token>();
        for (var i = 0; i < markerIndex; i++)
        {
            AppendRazorLine(tokens, lines[i], i > 0);
        }

        // Re-indent the inner snippet to the marker's position.
        tokens.Add(new Token(TokenKind.Punctuation, (markerIndex > 0 ? "\n" : string.Empty) + indent));
        foreach (var token in inner)
        {
            tokens.Add(token.Kind == TokenKind.Punctuation
                ? token with { Text = token.Text.Replace("\n", "\n" + indent) }
                : token);
        }

        for (var i = markerIndex + 1; i < lines.Length; i++)
        {
            AppendRazorLine(tokens, lines[i], newLine: true);
        }

        return tokens;
    }

    private static List<Token> Tokenize(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options)
    {
        var attributes = new List<(string Name, string Value)>();

        // The generic closing is part of what renders: <MudDataGrid T="Person" …>.
        if (component.Type.IsConstructedGenericType)
        {
            var names = component.Type.GetGenericTypeDefinition().GetGenericArguments();
            var arguments = component.Type.GenericTypeArguments;
            for (var i = 0; i < names.Length; i++)
            {
                attributes.Add((names[i].Name, Shell.Workspace.ParameterSignature.TypeName(arguments[i])));
            }
        }

        string? childText = null;
        var childContentFromHost = false;
        string[]? childContentSource = null;
        var slotChildren = new List<(string Name, string[]? Source)>();
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot)
            {
                var userText = state.IsModified(parameter.Name)
                               && state.GetValue(parameter) is string { Length: > 0 } text
                    ? text
                    : null;
                if (parameter.Name == "ChildContent")
                {
                    if (userText is not null)
                    {
                        childText = EscapeContent(userText);
                    }
                    else if (SlotHasPreset(component, parameter, options))
                    {
                        childContentFromHost = true;
                        childContentSource = SlotSource(component, parameter, options);
                    }
                }
                else if (SlotHasPreset(component, parameter, options))
                {
                    slotChildren.Add((parameter.Name, SlotSource(component, parameter, options)));
                }

                continue;
            }

            if (parameter.Kind is ControlKind.Event)
            {
                continue;
            }

            if (parameter.Kind is ControlKind.Unsupported)
            {
                // A preset on a non-drivable parameter has no literal form — show it as the
                // field the host would declare (<MudDataGrid Items="@_items">).
                if (options is not null
                    && options.TryGetParameterPreset(component.Type, parameter.Name, out var opaque)
                    && opaque is not null)
                {
                    attributes.Add((parameter.Name,
                        options.TryGetParameterSource(component.Type, parameter.Name, out var src)
                            ? src
                            : $"@_{char.ToLowerInvariant(parameter.Name[0])}{parameter.Name[1..]}"));
                }

                continue;
            }

            if (state.IsModified(parameter.Name))
            {
                if (state.GetValue(parameter) is { } value)
                {
                    attributes.Add((parameter.Name, FormatValue(value)));
                }
            }
            else if (options is not null
                     && options.TryGetParameterPreset(component.Type, parameter.Name, out var preset)
                     && preset is not null)
            {
                attributes.Add((parameter.Name,
                    options.TryGetParameterSource(component.Type, parameter.Name, out var src)
                        ? src
                        : FormatValue(preset)));
            }
        }

        var tokens = new List<Token> { new(TokenKind.Punctuation, "<"), new(TokenKind.Tag, component.DisplayName) };
        var multiline = attributes.Count > 2;
        var indent = "\n" + new string(' ', component.DisplayName.Length + 2);
        for (var i = 0; i < attributes.Count; i++)
        {
            tokens.Add(new Token(TokenKind.Punctuation, i == 0 || !multiline ? " " : indent));
            tokens.Add(new Token(TokenKind.AttributeName, attributes[i].Name));
            tokens.Add(new Token(TokenKind.Punctuation, "=\""));
            tokens.Add(new Token(TokenKind.AttributeValue, attributes[i].Value));
            tokens.Add(new Token(TokenKind.Punctuation, "\""));
        }

        var inlineChild = slotChildren.Count == 0
                          && (childText is not null
                              || (childContentFromHost && (childContentSource is null || childContentSource.Length == 1)));
        if (inlineChild)
        {
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
            if (childText is not null)
            {
                tokens.Add(new Token(TokenKind.ChildContent, childText));
            }
            else if (childContentSource is { Length: 1 })
            {
                AppendRazorLine(tokens, childContentSource[0], newLine: false, emitLead: false);
            }
            else
            {
                tokens.Add(new Token(TokenKind.Comment, HostContentComment));
            }

            tokens.Add(new Token(TokenKind.Punctuation, "</"));
            tokens.Add(new Token(TokenKind.Tag, component.DisplayName));
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
        }
        else if (slotChildren.Count > 0 || childContentFromHost || childText is not null)
        {
            // Structured children each get their own line.
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
            if (childText is not null)
            {
                tokens.Add(new Token(TokenKind.Punctuation, "\n    "));
                tokens.Add(new Token(TokenKind.ChildContent, childText));
            }
            else if (childContentFromHost)
            {
                if (childContentSource is null)
                {
                    tokens.Add(new Token(TokenKind.Punctuation, "\n    "));
                    tokens.Add(new Token(TokenKind.Comment, HostContentComment));
                }
                else
                {
                    AppendSourceLines(tokens, childContentSource, "    ");
                }
            }

            foreach (var (slot, source) in slotChildren)
            {
                tokens.Add(new Token(TokenKind.Punctuation, "\n    <"));
                tokens.Add(new Token(TokenKind.Tag, slot));
                tokens.Add(new Token(TokenKind.Punctuation, ">"));
                if (source is null)
                {
                    tokens.Add(new Token(TokenKind.Comment, HostContentComment));
                    tokens.Add(new Token(TokenKind.Punctuation, "</"));
                }
                else
                {
                    AppendSourceLines(tokens, source, "        ");
                    tokens.Add(new Token(TokenKind.Punctuation, "\n    </"));
                }

                tokens.Add(new Token(TokenKind.Tag, slot));
                tokens.Add(new Token(TokenKind.Punctuation, ">"));
            }

            tokens.Add(new Token(TokenKind.Punctuation, "\n</"));
            tokens.Add(new Token(TokenKind.Tag, component.DisplayName));
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
        }
        else
        {
            tokens.Add(new Token(TokenKind.Punctuation, " />"));
        }

        return tokens;
    }

    private static bool SlotHasPreset(ComponentDescriptor component, ParameterDescriptor parameter, PlayBlazorOptions? options)
        => options is not null && options.TryGetSlotPreset(component.Type, parameter.Name, out _);

    /// <summary>Host-provided razor text for a slot, dedented and split into lines.</summary>
    private static string[]? SlotSource(ComponentDescriptor component, ParameterDescriptor parameter, PlayBlazorOptions? options)
    {
        if (options is null || !options.TryGetSlotSource(component.Type, parameter.Name, out var source))
        {
            return null;
        }

        var lines = Dedent(source);
        return lines.Length == 0 ? null : lines;
    }

    private static void AppendSourceLines(List<Token> tokens, string[]? lines, string indent)
    {
        foreach (var line in lines ?? [])
        {
            tokens.Add(new Token(TokenKind.Punctuation, "\n" + indent));
            AppendRazorLine(tokens, line, newLine: false, emitLead: false);
        }
    }

    /// <summary>
    /// Colorizes one line of host-provided razor with the same token kinds as generated
    /// code — a light lexer (tags, attribute="value" pairs, text), not a razor parser.
    /// </summary>
    private static void AppendRazorLine(List<Token> tokens, string line, bool newLine, bool emitLead = true)
    {
        if (emitLead)
        {
            tokens.Add(new Token(TokenKind.Punctuation, newLine ? "\n" : string.Empty));
        }

        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == '<')
            {
                var j = i + 1;
                if (j < line.Length && line[j] == '/')
                {
                    j++;
                }

                var nameStart = j;
                while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] is '.' or '_'))
                {
                    j++;
                }

                tokens.Add(new Token(TokenKind.Punctuation, line[i..nameStart]));
                tokens.Add(new Token(TokenKind.Tag, line[nameStart..j]));
                i = j;
                // attributes until '>'
                while (i < line.Length && line[i] != '>')
                {
                    if (char.IsLetter(line[i]))
                    {
                        var a = i;
                        while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] is '-' or '_'))
                        {
                            i++;
                        }

                        if (i + 1 < line.Length && line[i] == '=' && line[i + 1] == '"')
                        {
                            var close = line.IndexOf('"', i + 2);
                            close = close < 0 ? line.Length - 1 : close;
                            tokens.Add(new Token(TokenKind.AttributeName, line[a..i]));
                            tokens.Add(new Token(TokenKind.Punctuation, "=\""));
                            tokens.Add(new Token(TokenKind.AttributeValue, line[(i + 2)..close]));
                            tokens.Add(new Token(TokenKind.Punctuation, "\""));
                            i = close + 1;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.AttributeName, line[a..i]));
                        }
                    }
                    else
                    {
                        var p = i;
                        while (i < line.Length && line[i] != '>' && !char.IsLetter(line[i]))
                        {
                            i++;
                        }

                        tokens.Add(new Token(TokenKind.Punctuation, line[p..i]));
                    }
                }

                if (i < line.Length)
                {
                    tokens.Add(new Token(TokenKind.Punctuation, ">"));
                    i++;
                }
            }
            else
            {
                var t = i;
                while (t < line.Length && line[t] != '<')
                {
                    t++;
                }

                var text = line[i..t];
                tokens.Add(new Token(
                    text.Trim().Length == 0 ? TokenKind.Punctuation : TokenKind.ChildContent, text));
                i = t;
            }
        }
    }

    private static string[] Dedent(string source)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n')
            .SkipWhile(string.IsNullOrWhiteSpace)
            .Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse()
            .ToArray();
        if (lines.Length == 0)
        {
            return lines;
        }

        var indent = lines.Where(static l => l.Trim().Length > 0)
            .Min(static l => l.Length - l.TrimStart().Length);
        return lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()).ToArray();
    }

    private static string EscapeContent(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FormatValue(object value)
        => value switch
        {
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
            string text => text.Replace("\"", "&quot;"),
            Array array => string.Join(", ", array.Cast<object?>()
                .Select(static item => Convert.ToString(item, CultureInfo.InvariantCulture))),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
}
