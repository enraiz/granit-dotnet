namespace Granit.BlobStorage;

/// <summary>
/// Post-upload validation step in the blob validation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Validators run <b>after</b> the file lands on S3 (Direct-to-Cloud architecture).
/// The <see cref="BlobValidationContext"/> provides partial S3 access (range GET for magic bytes,
/// HEAD for metadata) — the full file stream is never buffered in the application server.
/// </para>
/// <para>
/// Register custom validators via <c>AddBlobValidator&lt;T&gt;()</c> in the DI setup.
/// Built-in validators provided by <c>Granit.BlobStorage</c>:
/// <list type="bullet">
///   <item><c>MagicBytesValidator</c> (Order = 10)</item>
///   <item><c>MaxSizeValidator</c> (Order = 20)</item>
/// </list>
/// </para>
/// </remarks>
public interface IBlobValidator
{
    /// <summary>
    /// Execution order within the validation pipeline. Lower values run first.
    /// The pipeline is fail-fast: processing stops at the first <see cref="BlobValidationResult.IsValid"/> = <c>false</c>.
    /// </summary>
    int Order { get; }

    /// <summary>Runs the validation step.</summary>
    /// <param name="context">Context providing access to the <see cref="BlobDescriptor"/> and S3 metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BlobValidationResult> ValidateAsync(BlobValidationContext context, CancellationToken cancellationToken = default);
}
