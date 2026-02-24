namespace Granit.Features.Definitions;

/// <summary>
/// Contract for declaring application features in code.
/// </summary>
/// <remarks>
/// Implement this interface and register the implementation with:
/// <code>services.AddFeatureDefinitions&lt;MyFeatureDefinitionProvider&gt;();</code>
/// </remarks>
/// <example>
/// <code>
/// public sealed class GuavaFeatureDefinitionProvider : IFeatureDefinitionProvider
/// {
///     public void Define(IFeatureDefinitionContext context)
///     {
///         FeatureGroupDefinition guava = context.AddGroup("Guava", "Guava Features");
///
///         guava.AddToggle(GuavaFeatures.VideoConsultation.Name, defaultValue: false);
///         guava.AddNumeric(GuavaFeatures.MaxPatientsCount.Name, defaultValue: 50, min: 1, max: 10_000);
///     }
/// }
/// </code>
/// </example>
public interface IFeatureDefinitionProvider
{
    /// <summary>
    /// Declares feature groups and features using <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The definition context for registering groups and features.</param>
    void Define(IFeatureDefinitionContext context);
}
