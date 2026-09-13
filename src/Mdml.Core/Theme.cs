namespace Mdml.Core;

public sealed class Theme
{
    public required string Name { get; init; }

    public required string Html { get; init; }

    public string? FilePath { get; init; }

    public bool IsBuiltIn => FilePath is null;
}
