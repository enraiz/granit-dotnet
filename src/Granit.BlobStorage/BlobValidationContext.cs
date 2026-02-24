namespace Granit.BlobStorage;

/// <summary>
/// Provides <see cref="IBlobValidator"/> instances with access to blob metadata
/// and partial S3 content without buffering the full file.
/// </summary>
public sealed class BlobValidationContext
{
    /// <summary>The descriptor being validated.</summary>
    public required BlobDescriptor Descriptor { get; init; }

    /// <summary>
    /// Actual size in bytes as reported by S3 HEAD.
    /// Available without reading the object body.
    /// </summary>
    public required long ActualSizeBytes { get; init; }

    /// <summary>
    /// Factory that opens a partial S3 range-GET stream (e.g. first 261 bytes for magic-bytes analysis).
    /// The stream is the caller's responsibility to dispose.
    /// </summary>
    public required Func<int, CancellationToken, Task<Stream>> OpenPartialStreamAsync { get; init; }
}
