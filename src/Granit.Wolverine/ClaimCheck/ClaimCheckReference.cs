namespace Granit.Wolverine.ClaimCheck;

/// <summary>
/// Lightweight reference to a payload stored externally via the Claim Check pattern.
/// Carried inside Wolverine messages instead of the full payload.
/// </summary>
/// <param name="ReferenceId">Unique identifier returned by <see cref="IClaimCheckStore.StoreAsync"/>.</param>
/// <param name="PayloadType">
/// Assembly-qualified or simple type name of the original payload,
/// used for deserialization on the consumer side.
/// </param>
/// <param name="ContentType">MIME content type of the stored data (default: <c>application/json</c>).</param>
public sealed record ClaimCheckReference(
    Guid ReferenceId,
    string PayloadType,
    string ContentType = "application/json")
{
    /// <summary>
    /// Creates a <see cref="ClaimCheckReference"/> for the specified payload type.
    /// </summary>
    /// <typeparam name="T">The type of the original payload.</typeparam>
    /// <param name="referenceId">The store reference identifier.</param>
    /// <returns>A typed claim check reference.</returns>
    public static ClaimCheckReference Create<T>(Guid referenceId)
        => new(referenceId, typeof(T).FullName ?? typeof(T).Name);
}
