namespace Granit.Features;

/// <summary>
/// Enforces numeric feature limits before mutating operations.
/// </summary>
/// <remarks>
/// Inject into Wolverine handlers or application services to guard against exceeding
/// the subscription limit for a <see cref="ValueTypes.FeatureValueType.Numeric"/> feature.
/// <example>
/// <code>
/// public sealed class CreatePatientHandler(IFeatureLimitGuard limitGuard, IPatientRepository patients)
/// {
///     public async Task HandleAsync(CreatePatientCommand cmd, CancellationToken cancellationToken)
///     {
///         long current = await patients.CountAsync(cancellationToken);
///         await limitGuard.CheckAsync(AcmeFeatures.MaxUsersCount.Name, current, cancellationToken);
///         // ... proceed with creation
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IFeatureLimitGuard
{
    /// <summary>
    /// Throws <see cref="Exceptions.FeatureLimitExceededException"/> (HTTP 403)
    /// if <paramref name="currentCount"/> has reached or exceeded the resolved numeric
    /// limit for <paramref name="featureName"/>.
    /// </summary>
    /// <param name="featureName">The numeric feature name (e.g., <c>"Acme.MaxUsersCount"</c>).</param>
    /// <param name="currentCount">Current number of existing resources.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CheckAsync(string featureName, long currentCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the resolved numeric limit for <paramref name="featureName"/>.
    /// </summary>
    Task<long> GetLimitAsync(string featureName, CancellationToken cancellationToken = default);
}
