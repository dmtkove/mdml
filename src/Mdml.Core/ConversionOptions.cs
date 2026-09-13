namespace Mdml.Core;

public sealed class ConversionOptions
{
    public required string InputPath { get; init; }

    public string? OutputPath { get; init; }

    public string Theme { get; init; } = "default";
}
