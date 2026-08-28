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
/// same token stream, so they can never drift apart.
/// </summary>
public static class RazorSnippetGenerator
{
    private enum TokenKind
    {
        Punctuation,
        Tag,
        AttributeName,
        AttributeValue,
        ChildContent,
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    public static string Generate(ComponentDescriptor component, PlaygroundState state)
        => string.Concat(Tokenize(component, state).Select(static t => t.Text));

    public static MarkupString GenerateMarkup(ComponentDescriptor component, PlaygroundState state)
    {
        var builder = new StringBuilder();
        foreach (var token in Tokenize(component, state))
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
            _ => "punct",
        };

    private static List<Token> Tokenize(ComponentDescriptor component, PlaygroundState state)
    {
        var attributes = new List<(string Name, string Value)>();
        string? childContent = null;
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot)
            {
                // Only the conventional ChildContent slot round-trips into the snippet, as element content.
                if (parameter.Name == "ChildContent"
                    && state.IsModified(parameter.Name)
                    && state.GetValue(parameter) is string { Length: > 0 } text)
                {
                    childContent = EscapeContent(text);
                }
                continue;
            }
            if (parameter.Kind is ControlKind.Event or ControlKind.Unsupported)
            {
                continue;
            }
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }
            if (state.GetValue(parameter) is not { } value)
            {
                continue;
            }

            attributes.Add((parameter.Name, FormatValue(value)));
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

        if (childContent is null)
        {
            tokens.Add(new Token(TokenKind.Punctuation, " />"));
        }
        else
        {
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
            tokens.Add(new Token(TokenKind.ChildContent, childContent));
            tokens.Add(new Token(TokenKind.Punctuation, "</"));
            tokens.Add(new Token(TokenKind.Tag, component.DisplayName));
            tokens.Add(new Token(TokenKind.Punctuation, ">"));
        }

        return tokens;
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
