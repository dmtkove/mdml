namespace Mdml.Core;

internal static class MermaidScripts
{
    public const string Html =
        """
        <style>
        pre.mermaid {
          background: transparent;
          text-align: center;
          overflow-x: auto;
        }
        pre.mermaid svg {
          max-width: 100%;
          height: auto;
        }
        </style>
        <script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
        <script>
          mermaid.initialize({ startOnLoad: true, securityLevel: "strict" });
        </script>
        """;
}
