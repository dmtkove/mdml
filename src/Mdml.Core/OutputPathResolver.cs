namespace Mdml.Core;

internal static class OutputPathResolver
{
    public static string Resolve(string inputPath, string? outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var htmlFileName = Path.GetFileNameWithoutExtension(inputPath) + ".html";
        var inputDirectory = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(inputDirectory, htmlFileName);
        }

        var fullOutput = Path.GetFullPath(outputPath);

        if (Directory.Exists(fullOutput) || EndsWithDirectorySeparator(outputPath))
        {
            return Path.Combine(fullOutput, htmlFileName);
        }

        return fullOutput;
    }

    private static bool EndsWithDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ||
        path.EndsWith(Path.AltDirectorySeparatorChar);
}
