namespace Granit.Features.Definitions;

/// <summary>
/// Abstract base class for declaring application features in code.
/// </summary>
/// <remarks>
/// Inherit from this class and override <see cref="Define"/> to declare feature groups
/// and individual features. Register the implementation with:
/// <code>services.AddFeatureDefinitions&lt;MyFeatureDefinitionProvider&gt;();</code>
/// </remarks>
/// <example>
/// <code>
/// public sealed class GuavaFeatureDefinitionProvider : FeatureDefinitionProvider
/// {
///     public override void Define(IFeatureDefinitionContext context)
///     {
///         FeatureGroupDefinition guava = context.AddGroup("Guava", "Guava Features");
///
///         guava.AddToggle(GuavaFeatures.VideoConsultation.Name, defaultValue: false);
///         guava.AddNumeric(GuavaFeatures.MaxPatientsCount.Name, defaultValue: 50, min: 1, max: 10_000);
///     }
/// }
/// </code>
/// </example>
public abstract class FeatureDefinitionProvider
{
    /// <summary>
    /// Declares feature groups and features using <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The definition context for registering groups and features.</param>
    public abstract void Define(IFeatureDefinitionContext context);
}
