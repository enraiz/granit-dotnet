using Microsoft.Extensions.Options;

namespace Granit.Persistence.MultiTenancy;

/// <summary>
/// Default <see cref="ITenantSchemaProvider"/> that derives the schema name from the
/// tenant identifier using the convention configured in <see cref="TenantSchemaOptions"/>.
/// </summary>
internal sealed class DefaultTenantSchemaProvider(IOptions<TenantSchemaOptions> options)
    : ITenantSchemaProvider
{
    private readonly TenantSchemaOptions _options = options.Value;

    /// <inheritdoc/>
    public ValueTask<string> GetSchemaNameAsync(Guid tenantId, CancellationToken ct = default)
    {
        string suffix = _options.NamingConvention switch
        {
            TenantSchemaNamingConvention.TenantId => tenantId.ToString("N"),
            TenantSchemaNamingConvention.TenantName =>
                throw new InvalidOperationException(
                    $"Convention '{TenantSchemaNamingConvention.TenantName}' requires a custom " +
                    $"'{nameof(ITenantSchemaProvider)}' implementation that resolves the tenant name."),
            TenantSchemaNamingConvention.Custom =>
                throw new InvalidOperationException(
                    $"Convention '{TenantSchemaNamingConvention.Custom}' requires a custom " +
                    $"'{nameof(ITenantSchemaProvider)}' implementation."),
            _ => throw new InvalidOperationException(
                $"Unknown naming convention: {_options.NamingConvention}"),
        };

        return ValueTask.FromResult(_options.Prefix + suffix);
    }
}
