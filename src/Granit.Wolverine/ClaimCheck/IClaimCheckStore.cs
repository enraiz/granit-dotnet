namespace Granit.Wolverine.ClaimCheck;

/// <summary>
/// Stores and retrieves message payloads externally (Claim Check pattern).
/// Large payloads are offloaded to an external store (blob storage, S3, Redis…)
/// and replaced by a lightweight reference in the message bus.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>soft dependency</b>: if no implementation is registered in the DI container,
/// the claim check functionality is simply unavailable. Handlers that need it should resolve
/// <c>IClaimCheckStore</c> via <c>IServiceProvider.GetService()</c> and check for <c>null</c>.
/// </para>
/// <para>
/// An <see cref="Internal.InMemoryClaimCheckStore"/> is available for development and testing.
/// Production applications should register a durable implementation backed by blob storage
/// or another persistent store.
/// </para>
/// </remarks>
public interface IClaimCheckStore
{
    /// <summary>
    /// Stores a payload and returns a unique reference identifier.
    /// </summary>
    /// <param name="data">The serialized payload bytes.</param>
    /// <param name="contentType">Optional MIME content type (default: <c>application/json</c>).</param>
    /// <param name="expiry">
    /// Optional TTL after which the payload may be garbage-collected.
    /// <c>null</c> means the store's default expiry applies.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique identifier that can be used to retrieve the payload later.</returns>
    Task<Guid> StoreAsync(
        ReadOnlyMemory<byte> data,
        string? contentType = null,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a previously stored payload by its reference identifier.
    /// </summary>
    /// <param name="referenceId">The identifier returned by <see cref="StoreAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payload bytes, or <c>null</c> if the reference does not exist or has expired.</returns>
    Task<byte[]?> RetrieveAsync(
        Guid referenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously stored payload. Idempotent: returns <c>false</c> if not found.
    /// </summary>
    /// <param name="referenceId">The identifier returned by <see cref="StoreAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the payload was deleted; <c>false</c> if it was not found.</returns>
    Task<bool> DeleteAsync(
        Guid referenceId,
        CancellationToken cancellationToken = default);
}
