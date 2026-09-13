namespace Mdml.Core;

public sealed class MarkdownContent
{
    public required string Title { get; init; }

    public required string HtmlFragment { get; init; }

    public bool ContainsMermaid { get; init; }
}
