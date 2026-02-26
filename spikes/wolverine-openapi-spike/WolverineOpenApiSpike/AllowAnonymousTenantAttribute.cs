namespace WolverineOpenApiSpike;

/// <summary>
/// Marks an endpoint as not requiring tenant context.
/// Used to test custom attribute propagation in OpenAPI transformers.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowAnonymousTenantAttribute : Attribute;
