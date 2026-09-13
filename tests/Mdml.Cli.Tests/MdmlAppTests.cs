using Mdml.Cli;
using Mdml.Core;

namespace Mdml.Cli.Tests;

public sealed class MdmlAppTests
{
    [Fact]
    public void Convert_WritesSiblingHtmlAndPrintsPath()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n\nWorld.\n");

        var result = Invoke(inputPath);

        var expectedOutput = Path.Combine(workspace.DirectoryPath, "README.html");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(expectedOutput, result.Stdout);
        Assert.True(string.IsNullOrEmpty(result.Stderr));
        Assert.True(File.Exists(expectedOutput));
        Assert.Contains("<title>Hello</title>", File.ReadAllText(expectedOutput));
    }

    [Fact]
    public void Convert_Quiet_PrintsNothing()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");

        var result = Invoke(inputPath, "--quiet");

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrEmpty(result.Stdout));
        Assert.True(File.Exists(Path.Combine(workspace.DirectoryPath, "README.html")));
    }

    [Fact]
    public void Convert_ThemeGithub_AppliesBuiltInTheme()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");

        var result = Invoke(inputPath, "--theme", "github");

        Assert.Equal(0, result.ExitCode);
        var html = File.ReadAllText(Path.Combine(workspace.DirectoryPath, "README.html"));
        Assert.Contains("markdown-body", html);
    }

    [Fact]
    public void Convert_MermaidFence_IncludesDiagramRuntime()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write(
            "flow.md",
            """
            # Flow

            ```mermaid
            graph TD
                A --> B
            ```
            """);

        var result = Invoke(inputPath);

        Assert.Equal(0, result.ExitCode);
        var html = File.ReadAllText(Path.Combine(workspace.DirectoryPath, "flow.html"));
        Assert.Contains("class=\"mermaid\"", html);
        Assert.Contains("mermaid.min.js", html);
        Assert.Contains("A --> B", html);
    }

    [Fact]
    public void Convert_OutputFile_WritesToExplicitPath()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Title\n");
        var outputPath = Path.Combine(workspace.DirectoryPath, "custom.html");

        var result = Invoke(inputPath, "--output", outputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(outputPath, result.Stdout);
        Assert.True(File.Exists(outputPath));
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "README.html")));
    }

    [Fact]
    public void Convert_MultipleFiles_WritesEachSibling()
    {
        using var workspace = new TempWorkspace();
        var one = workspace.Write("one.md", "# One\n");
        var two = workspace.Write("two.md", "# Two\n");

        var result = Invoke(one, two);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(workspace.DirectoryPath, "one.html")));
        Assert.True(File.Exists(Path.Combine(workspace.DirectoryPath, "two.html")));
        Assert.Contains("one.html", result.Stdout);
        Assert.Contains("two.html", result.Stdout);
    }

    [Fact]
    public void Convert_MultipleFiles_OutputDirectory()
    {
        using var workspace = new TempWorkspace();
        var one = workspace.Write("one.md", "# One\n");
        var two = workspace.Write("two.md", "# Two\n");
        var outputDir = Path.Combine(workspace.DirectoryPath, "site") + Path.DirectorySeparatorChar;

        var result = Invoke(one, two, "--output", outputDir);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(workspace.DirectoryPath, "site", "one.html")));
        Assert.True(File.Exists(Path.Combine(workspace.DirectoryPath, "site", "two.html")));
    }

    [Fact]
    public void Convert_MultipleFiles_OutputFile_FailsWithoutWriting()
    {
        using var workspace = new TempWorkspace();
        var one = workspace.Write("one.md", "# One\n");
        var two = workspace.Write("two.md", "# Two\n");
        var outputFile = Path.Combine(workspace.DirectoryPath, "all.html");

        var result = Invoke(one, two, "--output", outputFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("directory", result.Stderr);
        Assert.False(File.Exists(outputFile));
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "one.html")));
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "two.html")));
    }

    [Fact]
    public void Convert_MissingFile_ReturnsOne()
    {
        using var workspace = new TempWorkspace();
        var missing = Path.Combine(workspace.DirectoryPath, "nope.md");

        var result = Invoke(missing);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not found", result.Stderr);
        Assert.Empty(Directory.GetFiles(workspace.DirectoryPath, "*.html"));
    }

    [Fact]
    public void Convert_MissingTheme_ReturnsOne()
    {
        using var workspace = new TempWorkspace();
        var inputPath = workspace.Write("README.md", "# Hello\n");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = MdmlApp.Run(
            [inputPath, "--theme", "missing"],
            stdout,
            stderr,
            new ThemeResolver(workspace.DirectoryPath));

        Assert.Equal(1, exit);
        Assert.Contains("not found", stderr.ToString());
        Assert.False(File.Exists(Path.Combine(workspace.DirectoryPath, "README.html")));
    }

    [Fact]
    public void Convert_NoFiles_ReturnsOne()
    {
        var result = Invoke();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("No Markdown files specified", result.Stderr);
    }

    [Fact]
    public void ListThemes_PrintsBuiltIns()
    {
        var result = Invoke("--list-themes");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("default", result.Stdout);
        Assert.Contains("github", result.Stdout);
    }

    [Fact]
    public void Help_ReturnsZero()
    {
        var result = Invoke("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Convert Markdown", result.Stdout);
        Assert.Contains("Mermaid", result.Stdout);
        Assert.Contains("--theme", result.Stdout);
        Assert.Contains("--output", result.Stdout);
    }

    [Fact]
    public void Version_ReturnsZero()
    {
        var result = Invoke("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("0.1.0", result.Stdout);
    }

    private static (int ExitCode, string Stdout, string Stderr) Invoke(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = MdmlApp.Run(args, stdout, stderr);
        return (exit, stdout.ToString(), stderr.ToString());
    }
}

internal sealed class TempWorkspace : IDisposable
{
    public string DirectoryPath { get; } = Directory.CreateTempSubdirectory("mdml-cli-").FullName;

    public string Write(string fileName, string contents)
    {
        var path = Path.Combine(DirectoryPath, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
