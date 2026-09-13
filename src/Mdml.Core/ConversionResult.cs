namespace Mdml.Core;

public sealed class ConversionResult
{
    public required string Title { get; init; }

    public required string HtmlFragment { get; init; }

    public required string Html { get; init; }

    public required string OutputPath { get; init; }

    public required string ThemeName { get; init; }
}
