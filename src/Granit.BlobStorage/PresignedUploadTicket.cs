namespace Granit.BlobStorage;

/// <summary>
/// Short-lived token returned by <see cref="IBlobStorage.InitiateUploadAsync"/>.
/// </summary>
/// <remarks>
/// The frontend uses <see cref="UploadUrl"/> with <see cref="HttpMethod"/> (always <c>PUT</c>)
/// and must include every entry from <see cref="RequiredHeaders"/> in the HTTP request.
/// The ticket expires at <see cref="ExpiresAt"/>; S3 will return <c>403 Forbidden</c> after that.
/// </remarks>
/// <param name="BlobId">Stable identifier for polling status and requesting downloads.</param>
/// <param name="UploadUrl">Pre-signed S3 PUT URL; valid until <see cref="ExpiresAt"/>.</param>
/// <param name="HttpMethod">HTTP method to use; always <c>"PUT"</c> for S3 pre-signed uploads.</param>
/// <param name="ExpiresAt">UTC expiry of the pre-signed URL.</param>
/// <param name="RequiredHeaders">Headers the client must include verbatim in the PUT request (e.g. <c>Content-Type</c>).</param>
public sealed record PresignedUploadTicket(
    Guid BlobId,
    Uri UploadUrl,
    string HttpMethod,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> RequiredHeaders);
