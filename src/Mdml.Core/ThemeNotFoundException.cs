namespace Mdml.Core;

public sealed class ThemeNotFoundException : Exception
{
    public ThemeNotFoundException(string themeName, IReadOnlyList<string> availableThemes)
        : base(BuildMessage(themeName, availableThemes))
    {
        ThemeName = themeName;
        AvailableThemes = availableThemes;
    }

    public string ThemeName { get; }

    public IReadOnlyList<string> AvailableThemes { get; }

    private static string BuildMessage(string themeName, IReadOnlyList<string> availableThemes)
    {
        var available = availableThemes.Count == 0
            ? "(none)"
            : string.Join(", ", availableThemes);

        return $"Theme '{themeName}' was not found. Available themes: {available}.";
    }
}
