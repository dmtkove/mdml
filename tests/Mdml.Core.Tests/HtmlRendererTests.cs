namespace Mdml.Core.Tests;

public sealed class HtmlRendererTests
{
    private readonly HtmlRenderer _renderer = new();

    [Fact]
    public void Render_SubstitutesTitleContentAndCss()
    {
        const string template = """
            <title>{{title}}</title>
            <style>{{css}}</style>
            <main>{{content}}</main>
            """;

        var html = _renderer.Render(
            template,
            new MarkdownContent
            {
                Title = "My document",
                HtmlFragment = "<h1>My document</h1>",
            },
            css: "body { color: #111; }");

        Assert.Equal(
            """
            <title>My document</title>
            <style>body { color: #111; }</style>
            <main><h1>My document</h1></main>
            """,
            html);
    }

    [Fact]
    public void Render_LeavesCssEmptyByDefault()
    {
        var html = _renderer.Render(
            "style:{{css}};content:{{content}}",
            new MarkdownContent
            {
                Title = "T",
                HtmlFragment = "<p>Hi</p>",
            });

        Assert.Equal("style:;content:<p>Hi</p>", html);
    }

    [Fact]
    public void Render_HtmlEncodesTitle()
    {
        var html = _renderer.Render(
            "<title>{{title}}</title>",
            new MarkdownContent
            {
                Title = "A & B <C>",
                HtmlFragment = "<p>x</p>",
            });

        Assert.Equal("<title>A &amp; B &lt;C&gt;</title>", html);
    }

    [Fact]
    public void Render_DoesNotSubstitutePlaceholdersInsideContent()
    {
        var html = _renderer.Render(
            HtmlRenderer.DefaultTemplate,
            new MarkdownContent
            {
                Title = "Doc",
                HtmlFragment = "<p>{{title}} {{css}} {{content}}</p>",
            },
            css: "body{}");

        Assert.Contains("<title>Doc</title>", html);
        Assert.Contains("<p>{{title}} {{css}} {{content}}</p>", html);
        Assert.Contains("body{}", html);
        Assert.DoesNotContain("mermaid.min.js", html);
    }

    [Fact]
    public void Render_LeavesUnknownPlaceholdersInTemplate()
    {
        var html = _renderer.Render(
            "{{title}} {{unknown}} {{content}}",
            new MarkdownContent
            {
                Title = "Doc",
                HtmlFragment = "<p>Hi</p>",
            });

        Assert.Equal("Doc {{unknown}} <p>Hi</p>", html);
    }

    [Fact]
    public void DefaultTemplate_ProducesCompleteDocument()
    {
        var html = _renderer.Render(
            HtmlRenderer.DefaultTemplate,
            new MarkdownContent
            {
                Title = "Hello",
                HtmlFragment = "<h1>Hello</h1>\n<p>World.</p>\n",
            });

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<meta charset=\"utf-8\">", html);
        Assert.Contains("<title>Hello</title>", html);
        Assert.Contains("<main class=\"markdown-body\">", html);
        Assert.Contains("<h1>Hello</h1>", html);
        Assert.Contains("<p>World.</p>", html);
        Assert.DoesNotContain("{{title}}", html);
        Assert.DoesNotContain("{{content}}", html);
        Assert.DoesNotContain("{{css}}", html);
        Assert.DoesNotContain("{{scripts}}", html);
        Assert.DoesNotContain("mermaid.min.js", html);
    }

    [Fact]
    public void Render_SubstitutesScriptsWhenDocumentHasMermaid()
    {
        var html = _renderer.Render(
            "<body>{{content}}{{scripts}}</body>",
            new MarkdownContent
            {
                Title = "Doc",
                HtmlFragment = "<pre class=\"mermaid\">graph TD; A-->B;</pre>",
                ContainsMermaid = true,
            });

        Assert.Contains("<pre class=\"mermaid\">graph TD; A-->B;</pre>", html);
        Assert.Contains("mermaid.min.js", html);
        Assert.Contains("mermaid.initialize", html);
        Assert.DoesNotContain("{{scripts}}", html);
    }

    [Fact]
    public void Render_InjectsMermaidRuntimeWhenThemeHasNoScriptsPlaceholder()
    {
        var html = _renderer.Render(
            "<html><body>{{content}}</body></html>",
            new MarkdownContent
            {
                Title = "Doc",
                HtmlFragment = "<pre class=\"mermaid\">graph TD; A-->B;</pre>",
                ContainsMermaid = true,
            });

        var scriptIndex = html.IndexOf("mermaid.min.js", StringComparison.Ordinal);
        var bodyIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        Assert.True(scriptIndex >= 0);
        Assert.True(scriptIndex < bodyIndex);
    }
}
