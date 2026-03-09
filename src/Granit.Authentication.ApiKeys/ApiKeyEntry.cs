using Granit.Core.Domain;
using Granit.Core.MultiTenancy;

namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Represents an API key with its metadata, permissions, and lifecycle state.
/// </summary>
public class ApiKeyEntry : AuditedEntity, ISoftDeletable, IMultiTenant
{
    /// <summary>Display name of the API key (e.g., "Partenaire Labo X").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Type of the API key (Secret, Publishable, Webhook, Ephemeral).</summary>
    public ApiKeyType Type { get; set; }

    /// <summary>Target environment (<c>live</c>, <c>test</c>, <c>dev</c>).</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the raw secret. The raw secret is never stored.</summary>
    public string HashedKey { get; set; } = string.Empty;

    /// <summary>Prefix of the key for display purposes (e.g., <c>gk_live_sk_</c>).</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Last four characters of the raw secret for identification.</summary>
    public string LastFourChars { get; set; } = string.Empty;

    /// <summary>Permissions granted to this API key (e.g., <c>["Guava.Patients.Read"]</c>).</summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>Allowed CIDR ranges for IP whitelisting. Empty means no restriction.</summary>
    public List<string> AllowedCidrs { get; set; } = [];

    /// <summary>Expiration date. <c>null</c> means the key does not expire.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Timestamp of the last API call using this key.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>Timestamp when the key was revoked. <c>null</c> if still active.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Controls caching behavior for this key.</summary>
    public CacheBehavior CacheBehavior { get; set; }

    // ISoftDeletable
    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc/>
    public string? DeletedBy { get; set; }

    // IMultiTenant
    /// <inheritdoc/>
    public Guid? TenantId { get; set; }
}
