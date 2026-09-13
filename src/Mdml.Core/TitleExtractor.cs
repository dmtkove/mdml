using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mdml.Core;

internal static class TitleExtractor
{
    public static string Extract(MarkdownDocument document, string fallback)
    {
        var heading = document.Descendants<HeadingBlock>().FirstOrDefault();
        if (heading?.Inline is null)
        {
            return fallback;
        }

        var title = ExtractInlines(heading.Inline).Trim();
        return string.IsNullOrEmpty(title) ? fallback : title;
    }

    private static string ExtractInlines(ContainerInline inlines)
    {
        var text = new StringBuilder();

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content);
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
                case HtmlEntityInline entity:
                    text.Append(entity.Transcoded);
                    break;
                case ContainerInline container:
                    text.Append(ExtractInlines(container));
                    break;
            }
        }

        return text.ToString();
    }
}
