namespace Granit.Templating.Keys;

/// <summary>
/// Immutable key used to locate a template in the resolver chain.
/// </summary>
/// <param name="Name">
/// Logical template name. Convention: <c>"Module.TemplateName"</c>
/// (e.g. <c>"Acme.InvoiceB2B"</c>).
/// </param>
/// <param name="Culture">
/// Optional BCP 47 culture tag (e.g. <c>"fr"</c>, <c>"en-US"</c>).
/// <c>null</c> means culture-neutral (fallback for any culture).
/// </param>
/// <remarks>
/// The resolver chain applies the following fallback strategy per resolver (highest priority first):
/// <list type="number">
///   <item>(<paramref name="Name"/>, <paramref name="Culture"/>)</item>
///   <item>(<paramref name="Name"/>, <c>null</c>)</item>
/// </list>
/// Tenant scoping is handled inside each <see cref="Pipeline.ITemplateResolver"/> implementation.
/// </remarks>
public sealed record TemplateKey(string Name, string? Culture = null);
