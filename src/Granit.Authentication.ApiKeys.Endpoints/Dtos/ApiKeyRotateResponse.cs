namespace Granit.Authentication.ApiKeys.Endpoints.Dtos;

/// <summary>
/// Response returned after rotating an API key.
/// Contains the new raw secret (shown once) and the old key ID for reference.
/// </summary>
/// <param name="NewKeyId">The identifier of the newly created replacement key.</param>
/// <param name="RawSecret">The new API key secret (shown once).</param>
/// <param name="Prefix">The new key prefix.</param>
/// <param name="LastFourChars">Last four characters of the new key.</param>
/// <param name="OldKeyId">The identifier of the key that was revoked.</param>
public sealed record ApiKeyRotateResponse(
    Guid NewKeyId,
    string RawSecret,
    string Prefix,
    string LastFourChars,
    Guid OldKeyId);
