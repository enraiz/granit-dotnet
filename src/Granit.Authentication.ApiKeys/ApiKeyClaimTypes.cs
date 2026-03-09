namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Claim types used by the API key authentication handler.
/// </summary>
public static class ApiKeyClaimTypes
{
    /// <summary>Claim type for a granted permission.</summary>
    public const string Permission = "permission";

    /// <summary>Claim type indicating the actor kind (User, ExternalSystem, System).</summary>
    public const string ActorKind = "actor_kind";

    /// <summary>Claim type for the API key identifier.</summary>
    public const string ApiKeyId = "api_key_id";

    /// <summary>Claim type for the API key type (Secret, Publishable, etc.).</summary>
    public const string ApiKeyType = "api_key_type";

    /// <summary>Claim type for the target environment (live, test, dev).</summary>
    public const string Environment = "api_key_env";

    /// <summary>Claim type for the tenant identifier.</summary>
    public const string TenantId = "tenant_id";
}
