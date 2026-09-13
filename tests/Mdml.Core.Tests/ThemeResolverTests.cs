namespace Mdml.Core.Tests;

public sealed class ThemeResolverTests
{
    [Fact]
    public void Resolve_Default_ReturnsBuiltInTheme()
    {
        using var workspace = new TempWorkspace();
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve("default");

        Assert.True(theme.IsBuiltIn);
        Assert.Equal("default", theme.Name);
        Assert.Contains("{{title}}", theme.Html);
        Assert.Contains("{{content}}", theme.Html);
        Assert.Contains("{{css}}", theme.Html);
        Assert.Contains("{{scripts}}", theme.Html);
        Assert.Contains("system-ui", theme.Html);
    }

    [Fact]
    public void Resolve_Github_ReturnsBuiltInTheme()
    {
        using var workspace = new TempWorkspace();
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve("GitHub");

        Assert.True(theme.IsBuiltIn);
        Assert.Equal("github", theme.Name);
        Assert.Contains("markdown-body", theme.Html);
        Assert.Contains("{{scripts}}", theme.Html);
    }

    [Fact]
    public void Resolve_BlankName_UsesDefault()
    {
        using var workspace = new TempWorkspace();
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve("  ");

        Assert.Equal("default", theme.Name);
        Assert.True(theme.IsBuiltIn);
    }

    [Fact]
    public void Resolve_ExternalHtmlPath_LoadsFile()
    {
        using var workspace = new TempWorkspace();
        var path = workspace.Write("company.html", "<html>{{content}}</html>");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve(path);

        Assert.False(theme.IsBuiltIn);
        Assert.Equal("company", theme.Name);
        Assert.Equal(path, theme.FilePath);
        Assert.Equal("<html>{{content}}</html>", theme.Html);
    }

    [Fact]
    public void Resolve_UserThemeDirectory_FindsNamedTheme()
    {
        using var workspace = new TempWorkspace();
        var path = workspace.Write("company.html", "<html class=\"co\">{{content}}</html>");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve("company");

        Assert.False(theme.IsBuiltIn);
        Assert.Equal("company", theme.Name);
        Assert.Equal(path, theme.FilePath);
        Assert.Contains("class=\"co\"", theme.Html);
    }

    [Fact]
    public void Resolve_BuiltInWinsOverUserThemeWithSameName()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("github.html", "<html>user-override</html>");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var theme = resolver.Resolve("github");

        Assert.True(theme.IsBuiltIn);
        Assert.DoesNotContain("user-override", theme.Html);
    }

    [Fact]
    public void Resolve_MissingFilePath_ThrowsFileNotFound()
    {
        using var workspace = new TempWorkspace();
        var missing = Path.Combine(workspace.DirectoryPath, "nope.html");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var ex = Assert.Throws<FileNotFoundException>(() => resolver.Resolve(missing));
        Assert.Equal(Path.GetFullPath(missing), ex.FileName);
    }

    [Fact]
    public void Resolve_UnknownName_ThrowsThemeNotFound()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("company.html", "<html>{{content}}</html>");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var ex = Assert.Throws<ThemeNotFoundException>(() => resolver.Resolve("academic"));

        Assert.Equal("academic", ex.ThemeName);
        Assert.Contains("default", ex.AvailableThemes);
        Assert.Contains("github", ex.AvailableThemes);
        Assert.Contains("company", ex.AvailableThemes);
    }

    [Fact]
    public void ListNames_IncludesBuiltInsAndUserThemes()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("company.html", "<html>{{content}}</html>");
        var resolver = new ThemeResolver(workspace.DirectoryPath);

        var names = resolver.ListNames();

        Assert.Contains("default", names);
        Assert.Contains("github", names);
        Assert.Contains("company", names);
    }

    [Fact]
    public void GetDefaultUserThemesDirectory_UsesPlatformAppData()
    {
        var directory = ThemeResolver.GetDefaultUserThemesDirectory();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.Equal(Path.Combine(appData, "mdml", "themes"), directory);
    }
}
