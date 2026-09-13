namespace Mdml.Core;

internal static class BuiltInThemes
{
    internal const string ResourcePrefix = "mdml.themes.";

    public static IReadOnlyDictionary<string, string> Load()
    {
        var assembly = typeof(BuiltInThemes).Assembly;
        var themes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = resourceName[ResourcePrefix.Length..];
            var name = Path.GetFileNameWithoutExtension(fileName);
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            themes[name] = reader.ReadToEnd();
        }

        return themes;
    }
}
