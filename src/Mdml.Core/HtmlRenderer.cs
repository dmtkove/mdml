using System.Net;

namespace Mdml.Core;

public sealed class HtmlRenderer
{
    public const string DefaultTemplate =
        """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <title>{{title}}</title>
        <style>
        {{css}}
        </style>
        </head>
        <body>
        <main class="markdown-body">
        {{content}}
        </main>
        {{scripts}}
        </body>
        </html>
        """;

    public string Render(string template, MarkdownContent content, string? css = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(content);

        var title = WebUtility.HtmlEncode(content.Title);
        var fragment = content.HtmlFragment ?? string.Empty;
        var scripts = content.ContainsMermaid ? MermaidScripts.Html : string.Empty;

        var html = template
            .Replace("{{title}}", title, StringComparison.Ordinal)
            .Replace("{{css}}", css ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{scripts}}", scripts, StringComparison.Ordinal)
            .Replace("{{content}}", fragment, StringComparison.Ordinal);

        if (content.ContainsMermaid &&
            !template.Contains("{{scripts}}", StringComparison.Ordinal))
        {
            html = InjectBeforeBodyClose(html, scripts);
        }

        return html;
    }

    private static string InjectBeforeBodyClose(string html, string scripts)
    {
        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyClose < 0)
        {
            return html + Environment.NewLine + scripts;
        }

        return html.Insert(bodyClose, scripts + Environment.NewLine);
    }
}
