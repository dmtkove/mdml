namespace Mdml.Core;

internal static class MermaidFence
{
    public static bool IsMermaid(string? info)
    {
        if (string.IsNullOrWhiteSpace(info))
        {
            return false;
        }

        var language = info.Trim();
        var separator = language.IndexOfAny([' ', '\t']);
        if (separator >= 0)
        {
            language = language[..separator];
        }

        return language.Equals("mermaid", StringComparison.OrdinalIgnoreCase);
    }
}
