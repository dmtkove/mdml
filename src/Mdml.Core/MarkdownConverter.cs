using Markdig;
using Markdig.Syntax;

namespace Mdml.Core;

public sealed class MarkdownConverter
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use(new MermaidExtension())
        .Build();

    private readonly HtmlRenderer _htmlRenderer = new();
    private readonly ThemeResolver _themeResolver;

    public MarkdownConverter()
        : this(new ThemeResolver())
    {
    }

    public MarkdownConverter(ThemeResolver themeResolver)
    {
        ArgumentNullException.ThrowIfNull(themeResolver);
        _themeResolver = themeResolver;
    }

    public MarkdownContent Convert(string markdown, string fallbackTitle)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackTitle);

        var document = Markdown.Parse(markdown, _pipeline);
        var html = Markdown.ToHtml(document, _pipeline);
        var title = TitleExtractor.Extract(document, fallbackTitle);
        var containsMermaid = document
            .Descendants<FencedCodeBlock>()
            .Any(block => MermaidFence.IsMermaid(block.Info));

        return new MarkdownContent
        {
            Title = title,
            HtmlFragment = html,
            ContainsMermaid = containsMermaid,
        };
    }

    public ConversionResult ConvertFile(ConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.InputPath);

        if (!File.Exists(options.InputPath))
        {
            throw new FileNotFoundException($"Markdown file not found: {options.InputPath}", options.InputPath);
        }

        var theme = _themeResolver.Resolve(options.Theme);
        var markdown = File.ReadAllText(options.InputPath);
        var fallbackTitle = Path.GetFileNameWithoutExtension(options.InputPath);
        if (string.IsNullOrWhiteSpace(fallbackTitle))
        {
            fallbackTitle = "document";
        }

        var content = Convert(markdown, fallbackTitle);
        var outputPath = OutputPathResolver.Resolve(options.InputPath, options.OutputPath);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var html = _htmlRenderer.Render(theme.Html, content);
        WriteAtomically(outputPath, html);

        return new ConversionResult
        {
            Title = content.Title,
            HtmlFragment = content.HtmlFragment,
            Html = html,
            OutputPath = outputPath,
            ThemeName = theme.Name,
        };
    }

    private static void WriteAtomically(string outputPath, string html)
    {
        var tempPath = outputPath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, html);
            File.Move(tempPath, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
