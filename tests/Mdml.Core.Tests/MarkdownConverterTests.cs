namespace Mdml.Core.Tests;

public sealed class MarkdownConverterTests
{
    private readonly MarkdownConverter _converter = new();

    [Fact]
    public void Convert_RendersHeadingsEmphasisAndLists()
    {
        const string markdown = """
            # My document

            Hello **world**.

            - One
            - Two
            - Three
            """;

        var result = _converter.Convert(markdown, "fallback");

        Assert.Equal("My document", result.Title);
        Assert.Contains("<h1 id=\"my-document\">My document</h1>", result.HtmlFragment);
        Assert.Contains("<p>Hello <strong>world</strong>.</p>", result.HtmlFragment);
        Assert.Contains("<li>One</li>", result.HtmlFragment);
        Assert.Contains("<li>Two</li>", result.HtmlFragment);
        Assert.Contains("<li>Three</li>", result.HtmlFragment);
    }

    [Fact]
    public void Convert_RendersGfmTablesStrikethroughAndFencedCode()
    {
        const string markdown = """
            | A | B |
            |---|---|
            | 1 | 2 |

            This is ~~gone~~.

            ```csharp
            var x = 1;
            ```
            """;

        var result = _converter.Convert(markdown, "code");

        Assert.Contains("<table>", result.HtmlFragment);
        Assert.Contains("<th>A</th>", result.HtmlFragment);
        Assert.Contains("<td>1</td>", result.HtmlFragment);
        Assert.Contains("<del>gone</del>", result.HtmlFragment);
        Assert.Contains("<pre><code", result.HtmlFragment);
        Assert.Contains("var x = 1;", result.HtmlFragment);
    }

    [Fact]
    public void Convert_UsesFirstHeadingAsTitleEvenWhenNotH1()
    {
        const string markdown = """
            Intro paragraph.

            ## Section title

            Body.
            """;

        var result = _converter.Convert(markdown, "file-stem");

        Assert.Equal("Section title", result.Title);
    }

    [Fact]
    public void Convert_UsesFallbackTitleWhenDocumentHasNoHeading()
    {
        var result = _converter.Convert("Just a paragraph.", "README");

        Assert.Equal("README", result.Title);
        Assert.Contains("<p>Just a paragraph.</p>", result.HtmlFragment);
    }

    [Fact]
    public void Convert_StripsEmphasisWhenExtractingTitle()
    {
        var result = _converter.Convert("# Hello **world**", "fallback");

        Assert.Equal("Hello world", result.Title);
    }

    [Fact]
    public void ConvertFile_WritesSiblingHtml()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n\nWorld.\n");

        var result = _converter.ConvertFile(new ConversionOptions { InputPath = inputPath });

        var expectedOutput = Path.Combine(workspace.DirectoryPath, "README.html");
        Assert.Equal(expectedOutput, result.OutputPath);
        Assert.True(File.Exists(expectedOutput));
        Assert.Equal("Hello", result.Title);

        var html = File.ReadAllText(expectedOutput);
        Assert.Equal(html, result.Html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<title>Hello</title>", html);
        Assert.Contains("<h1 id=\"hello\">Hello</h1>", html);
        Assert.Contains("<p>World.</p>", html);
        Assert.DoesNotContain("{{title}}", html);
        Assert.DoesNotContain("{{content}}", html);
        Assert.DoesNotContain("{{css}}", html);
        Assert.Equal("default", result.ThemeName);
        Assert.Contains("system-ui", html);
        Assert.DoesNotContain("{{scripts}}", html);
        Assert.DoesNotContain("mermaid.min.js", html);
    }

    [Fact]
    public void ConvertFile_WritesToExplicitFilePath()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Title\n");
        var outputPath = Path.Combine(workspace.DirectoryPath, "custom.html");

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
        });

        Assert.Equal(outputPath, result.OutputPath);
        Assert.True(File.Exists(outputPath));
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "README.html")));
    }

    [Fact]
    public void ConvertFile_WritesIntoOutputDirectory()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("guide.md", "# Guide\n");
        var outputDir = Path.Combine(workspace.DirectoryPath, "out");
        Directory.CreateDirectory(outputDir);

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            OutputPath = outputDir,
        });

        var expectedOutput = Path.Combine(outputDir, "guide.html");
        Assert.Equal(expectedOutput, result.OutputPath);
        Assert.True(File.Exists(expectedOutput));
    }

    [Fact]
    public void ConvertFile_CreatesMissingOutputDirectory()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("notes.md", "# Notes\n");
        var outputDir = Path.Combine(workspace.DirectoryPath, "missing-out") + Path.DirectorySeparatorChar;

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            OutputPath = outputDir,
        });

        var expectedOutput = Path.Combine(workspace.DirectoryPath, "missing-out", "notes.html");
        Assert.Equal(expectedOutput, result.OutputPath);
        Assert.True(File.Exists(expectedOutput));
    }

    [Fact]
    public void ConvertFile_MissingInput_ThrowsFileNotFound()
    {
        using var workspace = new TempWorkspace();
        var missing = Path.Combine(workspace.DirectoryPath, "nope.md");

        var ex = Assert.Throws<FileNotFoundException>(() =>
            _converter.ConvertFile(new ConversionOptions { InputPath = missing }));

        Assert.Equal(missing, ex.FileName);
        Assert.Empty(Directory.GetFiles(workspace.DirectoryPath, "*.html"));
    }

    [Fact]
    public void ConvertFile_AppliesBuiltInGithubTheme()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            Theme = "github",
        });

        Assert.Equal("github", result.ThemeName);
        Assert.Contains("class=\"markdown-body\"", result.Html);
        Assert.Contains("#f6f8fa", result.Html);
        Assert.Contains("<title>Hello</title>", result.Html);
        Assert.Contains("<h1 id=\"hello\">Hello</h1>", result.Html);
    }

    [Fact]
    public void ConvertFile_AppliesExternalThemeFile()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");
        var themePath = workspace.Write(
            "brand.html",
            "<html><head><title>{{title}}</title></head><body class=\"brand\">{{content}}</body></html>");

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            Theme = themePath,
        });

        Assert.Equal("brand", result.ThemeName);
        Assert.Contains("class=\"brand\"", result.Html);
        Assert.Contains("<h1 id=\"hello\">Hello</h1>", result.Html);
    }

    [Fact]
    public void ConvertFile_MissingTheme_DoesNotWriteOutput()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");
        var converter = new MarkdownConverter(new ThemeResolver(workspace.DirectoryPath));

        var ex = Assert.Throws<ThemeNotFoundException>(() =>
            converter.ConvertFile(new ConversionOptions
            {
                InputPath = inputPath,
                Theme = "missing",
            }));

        Assert.Equal("missing", ex.ThemeName);
        Assert.Contains("default", ex.AvailableThemes);
        Assert.Contains("github", ex.AvailableThemes);
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "README.html")));
    }

    [Fact]
    public void Convert_RendersMermaidFenceAsDiagramBlock()
    {
        const string markdown = """
            # Diagram

            ```mermaid
            graph TD
                A[Start] --> B[End]
            ```

            ```csharp
            var x = 1;
            ```
            """;

        var result = _converter.Convert(markdown, "fallback");

        Assert.True(result.ContainsMermaid);
        Assert.Contains("<pre", result.HtmlFragment);
        Assert.Contains("class=\"mermaid\"", result.HtmlFragment);
        Assert.Contains("graph TD", result.HtmlFragment);
        Assert.Contains("A[Start] --> B[End]", result.HtmlFragment);
        Assert.DoesNotContain("language-mermaid", result.HtmlFragment);
        Assert.Contains("language-csharp", result.HtmlFragment);
    }

    [Fact]
    public void Convert_EscapesHtmlInMermaidSource()
    {
        const string markdown = """
            ```mermaid
            graph TD
                A["x<y"] --> B
            ```
            """;

        var result = _converter.Convert(markdown, "diagram");

        Assert.True(result.ContainsMermaid);
        Assert.Contains("x&lt;y", result.HtmlFragment);
    }

    [Fact]
    public void ConvertFile_IncludesMermaidRuntime()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write(
            "diagram.md",
            """
            # Flow

            ```mermaid
            flowchart LR
                A --> B
            ```
            """);

        var result = _converter.ConvertFile(new ConversionOptions { InputPath = inputPath });

        Assert.Contains("class=\"mermaid\"", result.Html);
        Assert.Contains("flowchart LR", result.Html);
        Assert.Contains("mermaid.min.js", result.Html);
        Assert.Contains("mermaid.initialize", result.Html);
        Assert.DoesNotContain("{{scripts}}", result.Html);
    }

    [Fact]
    public void ConvertFile_ExternalThemeWithoutScriptsPlaceholder_StillLoadsMermaid()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write(
            "diagram.md",
            """
            ```mermaid
            graph TD
                A --> B
            ```
            """);
        var themePath = workspace.Write(
            "bare.html",
            "<html><body>{{content}}</body></html>");

        var result = _converter.ConvertFile(new ConversionOptions
        {
            InputPath = inputPath,
            Theme = themePath,
        });

        var scriptIndex = result.Html.IndexOf("mermaid.min.js", StringComparison.Ordinal);
        var bodyIndex = result.Html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        Assert.True(scriptIndex >= 0 && scriptIndex < bodyIndex);
    }
}
