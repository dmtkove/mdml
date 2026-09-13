namespace Mdml.Core;

public sealed class ThemeResolver
{
    public const string DefaultThemeName = "default";

    private readonly IReadOnlyDictionary<string, string> _builtInThemes;

    public ThemeResolver(string? userThemesDirectory = null)
        : this(BuiltInThemes.Load(), userThemesDirectory)
    {
    }

    internal ThemeResolver(IReadOnlyDictionary<string, string> builtInThemes, string? userThemesDirectory)
    {
        ArgumentNullException.ThrowIfNull(builtInThemes);
        _builtInThemes = builtInThemes;
        UserThemesDirectory = userThemesDirectory ?? GetDefaultUserThemesDirectory();
    }

    public string UserThemesDirectory { get; }

    public static string GetDefaultUserThemesDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(appData))
        {
            appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");
        }

        return Path.Combine(appData, "mdml", "themes");
    }

    public Theme Resolve(string? nameOrPath)
    {
        var requested = string.IsNullOrWhiteSpace(nameOrPath)
            ? DefaultThemeName
            : nameOrPath.Trim();

        if (LooksLikeFilePath(requested))
        {
            return LoadFromFile(requested);
        }

        if (_builtInThemes.TryGetValue(requested, out var builtInHtml))
        {
            return new Theme
            {
                Name = CanonicalBuiltInName(requested),
                Html = builtInHtml,
            };
        }

        var userThemePath = Path.Combine(UserThemesDirectory, requested + ".html");
        if (File.Exists(userThemePath))
        {
            return LoadFromFile(userThemePath, requested);
        }

        throw new ThemeNotFoundException(requested, ListNames());
    }

    public IReadOnlyList<string> ListNames()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in _builtInThemes.Keys)
        {
            names.Add(name);
        }

        if (Directory.Exists(UserThemesDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(UserThemesDirectory, "*.html"))
            {
                names.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        return names.ToList();
    }

    private Theme LoadFromFile(string path, string? name = null)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Theme file not found: {fullPath}", fullPath);
        }

        return new Theme
        {
            Name = name ?? Path.GetFileNameWithoutExtension(fullPath),
            Html = File.ReadAllText(fullPath),
            FilePath = fullPath,
        };
    }

    private string CanonicalBuiltInName(string requested)
    {
        foreach (var name in _builtInThemes.Keys)
        {
            if (string.Equals(name, requested, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return requested;
    }

    private static bool LooksLikeFilePath(string value) =>
        value.Contains(Path.DirectorySeparatorChar) ||
        value.Contains(Path.AltDirectorySeparatorChar) ||
        value.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);
}
