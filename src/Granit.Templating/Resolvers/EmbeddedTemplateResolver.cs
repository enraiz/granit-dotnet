using System.Collections.Concurrent;
using System.Reflection;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;

namespace Granit.Templating.Resolvers;

/// <summary>
/// Resolves templates from embedded assembly resources.
/// </summary>
/// <remarks>
/// Lowest-priority resolver (priority <c>-100</c>) — serves as a code-level fallback
/// when no store-backed resolver finds a published template.
/// <para>
/// Resource naming convention (case-sensitive):
/// <list type="table">
///   <listheader><term>Culture</term><description>Resource name</description></listheader>
///   <item>
///     <term>Specific (<c>"fr"</c>)</term>
///     <description><c>{AssemblyName}.Templates.{TemplateName}.fr.html</c></description>
///   </item>
///   <item>
///     <term>Neutral (any / fallback)</term>
///     <description><c>{AssemblyName}.Templates.{TemplateName}.html</c></description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Register via:
/// <code>
/// services.AddEmbeddedTemplates(typeof(GuavaTemplates).Assembly);
/// </code>
/// Multiple assemblies can be registered by calling <c>AddEmbeddedTemplates</c> multiple times.
/// </para>
/// </remarks>
internal sealed class EmbeddedTemplateResolver(IReadOnlyList<Assembly> assemblies) : ITemplateResolver
{
    private readonly IReadOnlyList<Assembly> _assemblies = assemblies;

    // Cache resource names per assembly to avoid repeated GetManifestResourceNames() allocations
    private readonly ConcurrentDictionary<Assembly, HashSet<string>> _resourceNameCache = new();

    /// <inheritdoc/>
    public int Priority => -100;

    /// <inheritdoc/>
    public Task<TemplateDescriptor?> TryResolveAsync(
        TemplateKey key, CancellationToken ct = default)
    {
        foreach (Assembly assembly in _assemblies)
        {
            TemplateDescriptor? descriptor = TryLoadFromAssembly(assembly, key);
            if (descriptor is not null)
            {
                return Task.FromResult<TemplateDescriptor?>(descriptor);
            }
        }

        return Task.FromResult<TemplateDescriptor?>(null);
    }

    private TemplateDescriptor? TryLoadFromAssembly(Assembly assembly, TemplateKey key)
    {
        string assemblyName = assembly.GetName().Name ?? string.Empty;

        // Culture-specific resource first, then neutral fallback
        string? resourceName = key.Culture is not null
            ? FindResource(assembly, $"{assemblyName}.Templates.{key.Name}.{key.Culture}.html")
                ?? FindResource(assembly, $"{assemblyName}.Templates.{key.Name}.html")
            : FindResource(assembly, $"{assemblyName}.Templates.{key.Name}.html");

        if (resourceName is null)
        {
            return null;
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream, System.Text.Encoding.UTF8);
        string content = reader.ReadToEnd();

        return new TemplateDescriptor
        {
            Content = content,
            MimeType = "text/html",
            RevisionId = null,
        };
    }

    private string? FindResource(Assembly assembly, string resourceName)
    {
        HashSet<string> names = _resourceNameCache.GetOrAdd(
            assembly,
            static a => new HashSet<string>(a.GetManifestResourceNames(), StringComparer.Ordinal));

        return names.Contains(resourceName) ? resourceName : null;
    }
}
