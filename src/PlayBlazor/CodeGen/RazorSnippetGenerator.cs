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

    public static string Generate(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options = null)
        => string.Concat(Tokenize(component, state, options).Select(static t => t.Text));

    public static MarkupString GenerateMarkup(ComponentDescriptor component, PlaygroundState state, PlayBlazorOptions? options = null)
    {
        var builder = new StringBuilder();
        foreach (var token in Tokenize(component, state, options))
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
        var slotChildren = new List<string>();
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
                    }
                }
                else if (SlotHasPreset(component, parameter, options))
                {
                    slotChildren.Add(parameter.Name);
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
                    attributes.Add((parameter.Name, $"@_{char.ToLowerInvariant(parameter.Name[0])}{parameter.Name[1..]}"));
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
                attributes.Add((parameter.Name, FormatValue(preset)));
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

        if (slotChildren.Count > 0)
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
                tokens.Add(new Token(TokenKind.Punctuation, "\n    "));
                tokens.Add(new Token(TokenKind.Comment, HostContentComment));
            }

            foreach (var slot in slotChildren)
            {
                tokens.Add(new Token(TokenKind.Punctuation, "\n    <"));
                tokens.Add(new Token(TokenKind.Tag, slot));
                tokens.Add(new Token(TokenKind.Punctuation, ">"));
                tokens.Add(new Token(TokenKind.Comment, HostContentComment));
                tokens.Add(new Token(TokenKind.Punctuation, "</"));
                tokens.Add(new Token(TokenKind.Tag, slot));
                tokens.Add(new Token(TokenKind.Punctuation, ">"));
            }

            tokens.Add(new Token(TokenKind.Punctuation, "\n</"));
            tokens.Add(new Token(TokenKind.Tag, component.DisplayName));
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
        }
        else if (childText is not null || childContentFromHost)
        {
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
            tokens.Add(childText is not null
                ? new Token(TokenKind.ChildContent, childText)
                : new Token(TokenKind.Comment, HostContentComment));
            tokens.Add(new Token(TokenKind.Punctuation, "</"));
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
