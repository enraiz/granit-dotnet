// ---------------------------------------------------------------------------
// JsonStringLocalizerFactory.cs
// Implements IStringLocalizerFactory to create JsonStringLocalizer instances.
// Thread-safe cache via ConcurrentDictionary<Type, Lazy<IStringLocalizer>>.
// Resolves resource inheritance and registered JSON sources.
// ---------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Granit.Localization.Json;

/// <summary>
/// JSON localizer factory based on resources registered in
/// <see cref="GranitLocalizationOptions"/>.
/// </summary>
internal sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<Type, Lazy<IStringLocalizer>> _cache = new();
    private readonly IOptions<GranitLocalizationOptions> _options;

    public JsonStringLocalizerFactory(IOptions<GranitLocalizationOptions> options)
    {
        _options = options;

        if (options.Value.EnableAutoDiscovery)
        {
            LocalizationAutoDiscovery.Discover(options.Value);
        }
    }

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        Lazy<IStringLocalizer> lazy = _cache.GetOrAdd(
            resourceSource,
            type => new Lazy<IStringLocalizer>(() => CreateLocalizer(type)));

        return lazy.Value;
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
    {
        // Try to resolve the type from the fully-qualified name
        var resourceType = Type.GetType($"{baseName}, {location}");
        if (resourceType is not null)
        {
            return Create(resourceType);
        }

        // Fallback: create an empty localizer (key = returned value)
        return new JsonStringLocalizer([], "fr", []);
    }

    /// <summary>
    /// Creates a localizer for the given resource type, with inheritance.
    /// </summary>
    private JsonStringLocalizer CreateLocalizer(Type resourceType)
    {
        GranitLocalizationOptions options = _options.Value;

        if (!options.Resources.TryGetValue(resourceType, out LocalizationResourceInfo? info))
        {
            // Unregistered type: return an empty localizer
            return new JsonStringLocalizer([], "fr", []);
        }

        // Build localizers for parent resources (recursive)
        List<IStringLocalizer> baseLocalizers = [];
        foreach (Type baseType in info.BaseTypes)
        {
            baseLocalizers.Add(Create(baseType));
        }

        // Check [InheritResource] attributes on the marker class
        var inheritAttributes =
            (Attributes.InheritResourceAttribute[])resourceType
                .GetCustomAttributes(typeof(Attributes.InheritResourceAttribute), true);

        foreach (Attributes.InheritResourceAttribute attr in inheritAttributes)
        {
            foreach (Type baseResourceType in attr.BaseResourceTypes.Where(t => !info.BaseTypes.Contains(t)))
            {
                baseLocalizers.Add(Create(baseResourceType));
            }
        }

        return new JsonStringLocalizer(info.JsonSources, info.DefaultCulture, baseLocalizers);
    }
}
