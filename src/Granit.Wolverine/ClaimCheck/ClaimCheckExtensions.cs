using System.Text.Json;

namespace Granit.Wolverine.ClaimCheck;

/// <summary>
/// Convenience extensions for <see cref="IClaimCheckStore"/> that handle
/// JSON serialization and deserialization of typed payloads.
/// </summary>
public static class ClaimCheckExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes <paramref name="payload"/> to JSON and stores it in the claim check store.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="store">The claim check store.</param>
    /// <param name="payload">The payload to store.</param>
    /// <param name="expiry">Optional TTL for the stored payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ClaimCheckReference"/> pointing to the stored payload.</returns>
    public static async Task<ClaimCheckReference> StorePayloadAsync<T>(
        this IClaimCheckStore store,
        T payload,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(payload);

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload, s_jsonOptions);
        Guid referenceId = await store.StoreAsync(json, "application/json", expiry, cancellationToken)
            .ConfigureAwait(false);

        return ClaimCheckReference.Create<T>(referenceId);
    }

    /// <summary>
    /// Retrieves and deserializes a payload previously stored via
    /// <see cref="StorePayloadAsync{T}"/>.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="store">The claim check store.</param>
    /// <param name="reference">The claim check reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized payload, or <c>null</c> if the reference has expired or was deleted.</returns>
    public static async Task<T?> RetrievePayloadAsync<T>(
        this IClaimCheckStore store,
        ClaimCheckReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(reference);

        byte[]? data = await store.RetrieveAsync(reference.ReferenceId, cancellationToken)
            .ConfigureAwait(false);

        return data is not null
            ? JsonSerializer.Deserialize<T>(data, s_jsonOptions)
            : default;
    }

    /// <summary>
    /// Retrieves, deserializes, and deletes a payload in one operation (consume-once semantics).
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="store">The claim check store.</param>
    /// <param name="reference">The claim check reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized payload, or <c>null</c> if the reference has expired or was deleted.</returns>
    public static async Task<T?> ConsumePayloadAsync<T>(
        this IClaimCheckStore store,
        ClaimCheckReference reference,
        CancellationToken cancellationToken = default)
    {
        T? payload = await store.RetrievePayloadAsync<T>(reference, cancellationToken).ConfigureAwait(false);

        if (payload is not null)
        {
            await store.DeleteAsync(reference.ReferenceId, cancellationToken).ConfigureAwait(false);
        }

        return payload;
    }
}
