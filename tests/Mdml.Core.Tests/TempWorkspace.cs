namespace Mdml.Core.Tests;

internal sealed class TempWorkspace : IDisposable
{
    public string DirectoryPath { get; } = Directory.CreateTempSubdirectory("mdml-").FullName;

    public string Write(string fileName, string contents)
    {
        var path = Path.Combine(DirectoryPath, fileName);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
