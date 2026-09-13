using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Mdml.Core;

namespace Mdml.Cli;

public static class MdmlApp
{
    public static int Run(
        string[] args,
        TextWriter? stdout = null,
        TextWriter? stderr = null,
        ThemeResolver? themeResolver = null)
    {
        stdout ??= Console.Out;
        stderr ??= Console.Error;
        themeResolver ??= new ThemeResolver();

        var command = CreateCommand(stdout, stderr, themeResolver);
        var parseResult = command.Parse(args);

        return parseResult.Invoke(new InvocationConfiguration
        {
            Output = stdout,
            Error = stderr,
            EnableDefaultExceptionHandler = false,
        });
    }

    private static RootCommand CreateCommand(TextWriter stdout, TextWriter stderr, ThemeResolver themeResolver)
    {
        var filesArgument = new Argument<string[]>("files")
        {
            Description = "Markdown files to convert.",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var themeOption = new Option<string>("--theme", "-t")
        {
            Description = "Built-in theme name (default, github) or path to a theme HTML file.",
            DefaultValueFactory = _ => ThemeResolver.DefaultThemeName,
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output HTML file (single input) or directory (one or more inputs).",
        };

        var quietOption = new Option<bool>("--quiet", "-q")
        {
            Description = "Do not print written file paths.",
        };

        var listThemesOption = new Option<bool>("--list-themes")
        {
            Description = "List built-in and user theme names.",
        };

        var versionOption = new VersionOption
        {
            Action = new ShowVersionAction(),
        };

        var rootCommand = new RootCommand("Convert Markdown files to themed HTML documents. Mermaid diagrams in fenced code blocks are rendered in the browser.");
        foreach (var option in rootCommand.Options.OfType<VersionOption>().ToArray())
        {
            rootCommand.Options.Remove(option);
        }

        rootCommand.Arguments.Add(filesArgument);
        rootCommand.Options.Add(themeOption);
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(quietOption);
        rootCommand.Options.Add(listThemesOption);
        rootCommand.Options.Add(versionOption);

        rootCommand.SetAction(parseResult =>
        {
            if (parseResult.GetValue(listThemesOption))
            {
                foreach (var name in themeResolver.ListNames())
                {
                    stdout.WriteLine(name);
                }

                return 0;
            }

            var files = parseResult.GetValue(filesArgument) ?? [];
            var theme = parseResult.GetValue(themeOption) ?? ThemeResolver.DefaultThemeName;
            var output = parseResult.GetValue(outputOption);
            var quiet = parseResult.GetValue(quietOption);

            return ConvertFiles(files, theme, output, quiet, stdout, stderr, themeResolver);
        });

        return rootCommand;
    }

    private static int ConvertFiles(
        string[] files,
        string theme,
        string? output,
        bool quiet,
        TextWriter stdout,
        TextWriter stderr,
        ThemeResolver themeResolver)
    {
        if (files.Length == 0)
        {
            stderr.WriteLine("mdml: No Markdown files specified.");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(output) && files.Length > 1 && !IsDirectoryOutput(output))
        {
            stderr.WriteLine("mdml: --output must be a directory when converting more than one file.");
            return 1;
        }

        var converter = new MarkdownConverter(themeResolver);

        try
        {
            foreach (var file in files)
            {
                var result = converter.ConvertFile(new ConversionOptions
                {
                    InputPath = file,
                    OutputPath = output,
                    Theme = theme,
                });

                if (!quiet)
                {
                    stdout.WriteLine(result.OutputPath);
                }
            }

            return 0;
        }
        catch (Exception ex) when (ex is FileNotFoundException or ThemeNotFoundException or IOException or ArgumentException or UnauthorizedAccessException)
        {
            stderr.WriteLine($"mdml: {ex.Message}");
            return 1;
        }
    }

    private static string GetVersion()
    {
        var assembly = typeof(MdmlApp).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    }

    private sealed class ShowVersionAction : SynchronousCommandLineAction
    {
        public override bool Terminating => true;

        public override bool ClearsParseErrors => true;

        public override int Invoke(ParseResult parseResult)
        {
            parseResult.InvocationConfiguration.Output.WriteLine(GetVersion());
            return 0;
        }
    }

    private static bool IsDirectoryOutput(string outputPath) =>
        Directory.Exists(outputPath) ||
        outputPath.EndsWith(Path.DirectorySeparatorChar) ||
        outputPath.EndsWith(Path.AltDirectorySeparatorChar);
}
