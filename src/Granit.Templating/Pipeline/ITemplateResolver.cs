using Granit.Templating.Keys;

namespace Granit.Templating.Pipeline;

/// <summary>
/// Resolves a template source from a storage backend given a <see cref="TemplateKey"/>.
/// </summary>
/// <remarks>
/// Multiple resolvers are chained by descending <see cref="Priority"/>. The first resolver
/// that returns a non-<c>null</c> descriptor wins.
/// <para>
/// Built-in resolvers and their priorities:
/// <list type="table">
///   <listheader><term>Resolver</term><description>Priority</description></listheader>
///   <item><term><c>StoreTemplateResolver</c> (EF Core)</term><description>100</description></item>
///   <item><term><c>EmbeddedTemplateResolver</c> (assembly resources)</term><description>-100</description></item>
/// </list>
/// </para>
/// <para>
/// Tenant scoping is the resolver's responsibility. A store-backed resolver should first
/// look for a tenant-scoped template, then fall back to the host-level one.
/// </para>
/// </remarks>
public interface ITemplateResolver
{
    /// <summary>
    /// Resolution priority. Higher values are tried first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to locate and return the template for the given key.
    /// </summary>
    /// <param name="key">The template key (name + optional culture).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TemplateDescriptor"/> when a matching template is found;
    /// <c>null</c> when this resolver has no template for the key.
    /// </returns>
    Task<TemplateDescriptor?> TryResolveAsync(TemplateKey key, CancellationToken ct = default);
}
