using Granit.Templating.GlobalContext;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Scriban.Exceptions;
using Scriban;
using Scriban.Runtime;

namespace Granit.Templating.Scriban;

/// <summary>
/// <see cref="ITemplateEngine"/> implementation backed by
/// <a href="https://github.com/scriban/scriban">Scriban</a>.
/// Supports <c>text/html</c> and <c>text/plain</c> MIME types.
/// </summary>
/// <remarks>
/// <strong>Security:</strong> templates run in a sandboxed <see cref="TemplateContext"/> with
/// <c>EnableRelaxedMemberAccess = false</c>. No I/O, reflection, or .NET assembly access is
/// available from within a template.
/// <para>
/// Model data is exposed under the <c>model</c> variable with snake_case property names
/// (e.g. <c>{{ model.first_name }}</c>). Global contexts are injected under their
/// <see cref="ITemplateGlobalContext.ContextName"/> (e.g. <c>{{ now.date }}</c>).
/// </para>
/// </remarks>
internal sealed class ScribanTemplateEngine : ITemplateEngine
{
    /// <inheritdoc/>
    public bool CanRender(TemplateDescriptor descriptor) =>
        string.Equals(descriptor.MimeType, "text/html", StringComparison.OrdinalIgnoreCase)
        || string.Equals(descriptor.MimeType, "text/plain", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<RenderedContent> RenderAsync<TData>(
        TemplateDescriptor descriptor,
        TData data,
        DocumentFormat targetFormat,
        IReadOnlyList<ITemplateGlobalContext> globalContexts,
        CancellationToken ct = default) where TData : notnull
    {
        Template template = Template.Parse(descriptor.Content);
        if (template.HasErrors)
        {
            throw new TemplateParseException(template.Messages);
        }

        TemplateContext context = BuildContext(data, globalContexts, ct);
        string rendered = template.Render(context);

        RenderedContent result = new TextRenderedContent(rendered, targetFormat)
        {
            RevisionId = descriptor.RevisionId,
        };

        return Task.FromResult(result);
    }

    private static TemplateContext BuildContext<TData>(
        TData data,
        IReadOnlyList<ITemplateGlobalContext> globalContexts,
        CancellationToken ct) where TData : notnull
    {
        ScriptObject globals = [];

        // Expose TData as "model" with snake_case property names (PascalCase → snake_case)
        ScriptObject model = [];
        model.Import(data, renamer: StandardMemberRenamer.Default);
        globals.SetValue("model", model, readOnly: true);

        // Inject each global context under its ContextName
        foreach (ITemplateGlobalContext globalContext in globalContexts)
        {
            ScriptObject contextObj = [];
            contextObj.Import(globalContext.Resolve(), renamer: StandardMemberRenamer.Default);
            globals.SetValue(globalContext.ContextName, contextObj, readOnly: true);
        }

        TemplateContext templateContext = new(globals)
        {
            // Sandboxing: no bypass of member visibility restrictions
            EnableRelaxedMemberAccess = false,

            // Propagate cancellation to the Scriban render loop
            CancellationToken = ct,
        };

        return templateContext;
    }
}
