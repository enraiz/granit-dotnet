namespace Granit.BlobStorage;

/// <summary>
/// Outcome of a single <see cref="IBlobValidator"/> step.
/// </summary>
public sealed record BlobValidationResult
{
    private BlobValidationResult() { }

    /// <summary>Whether this validation step passed.</summary>
    public bool IsValid { get; private init; }

    /// <summary>Human-readable failure reason; <c>null</c> when <see cref="IsValid"/> is <c>true</c>.</summary>
    public string? FailureReason { get; private init; }

    /// <summary>
    /// Content-Type confirmed by the validator (e.g. from magic-bytes analysis).
    /// Only set by <c>MagicBytesValidator</c>; other validators leave this <c>null</c>.
    /// </summary>
    public string? VerifiedContentType { get; private init; }

    /// <summary>Creates a successful validation result.</summary>
    public static BlobValidationResult Success(string? verifiedContentType = null) =>
        new() { IsValid = true, VerifiedContentType = verifiedContentType };

    /// <summary>Creates a failed validation result.</summary>
    public static BlobValidationResult Failure(string reason) =>
        new() { IsValid = false, FailureReason = reason };
}
