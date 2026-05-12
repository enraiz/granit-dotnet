namespace Granit.BlobStorage.Validators;

/// <summary>
/// Validates that the actual S3 object size does not exceed the maximum declared at upload time.
/// </summary>
/// <remarks>
/// <para>
/// Order: <b>20</b>. Runs after <see cref="MagicBytesValidator"/> (Order=10).
/// </para>
/// <para>
/// The limit (<see cref="Granit.BlobStorage.Domain.BlobDescriptor.MaxAllowedBytes"/>) is set per upload request and persisted
/// in the <see cref="Granit.BlobStorage.Domain.BlobDescriptor"/>. This prevents a client from bypassing the declared limit
/// by uploading a larger file directly to the pre-signed S3 URL.
/// </para>
/// </remarks>
public sealed class MaxSizeValidator : IBlobValidator
{
    /// <inheritdoc/>
    public int Order => 20;

    /// <inheritdoc/>
    public Task<BlobValidationResult> ValidateAsync(
        BlobValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.ActualSizeBytes <= context.Descriptor.MaxAllowedBytes)
        {
            return Task.FromResult(BlobValidationResult.Success());
        }

        return Task.FromResult(BlobValidationResult.Failure(
            $"File size {context.ActualSizeBytes:N0} bytes exceeds the allowed maximum of " +
            $"{context.Descriptor.MaxAllowedBytes:N0} bytes."));
    }
}
