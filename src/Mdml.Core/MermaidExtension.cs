using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Mdml.Core;

internal sealed class MermaidExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not Markdig.Renderers.HtmlRenderer htmlRenderer)
        {
            return;
        }

        var codeRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
        if (codeRenderer is null)
        {
            codeRenderer = new CodeBlockRenderer();
            htmlRenderer.ObjectRenderers.AddIfNotAlready(codeRenderer);
        }

        codeRenderer.BlockMapping["mermaid"] = "pre";
    }
}
