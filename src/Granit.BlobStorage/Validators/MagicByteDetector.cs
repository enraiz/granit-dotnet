namespace Granit.BlobStorage.Validators;

/// <summary>
/// Detects MIME types from binary magic-byte signatures.
/// Supports common document and medical imaging formats.
/// </summary>
internal static class MagicByteDetector
{
    /// <summary>
    /// Minimum byte count required for reliable detection.
    /// 261 bytes covers all supported signatures, including DICOM (magic at offset 128).
    /// </summary>
    internal const int RequiredByteCount = 261;

    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46];           // %PDF
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];                // JPEG SOI
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]; // PNG
    private static readonly byte[] Gif87aSignature = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61]; // GIF87a
    private static readonly byte[] Gif89aSignature = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]; // GIF89a
    private static readonly byte[] TiffLeSignature = [0x49, 0x49, 0x2A, 0x00];        // TIFF little-endian
    private static readonly byte[] TiffBeSignature = [0x4D, 0x4D, 0x00, 0x2A];        // TIFF big-endian
    private static readonly byte[] DicomSignature = [0x44, 0x49, 0x43, 0x4D];         // DICM (at offset 128)
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];           // PK ZIP

    /// <summary>
    /// Detects the MIME type from the leading bytes of a binary buffer.
    /// Returns <c>null</c> if no known signature matches.
    /// </summary>
    internal static string? Detect(ReadOnlySpan<byte> buffer)
    {
        if (MatchesAt(buffer, 0, PdfSignature)) { return "application/pdf"; }
        if (MatchesAt(buffer, 0, JpegSignature)) { return "image/jpeg"; }
        if (MatchesAt(buffer, 0, PngSignature)) { return "image/png"; }
        if (MatchesAt(buffer, 0, Gif87aSignature)) { return "image/gif"; }
        if (MatchesAt(buffer, 0, Gif89aSignature)) { return "image/gif"; }
        if (MatchesAt(buffer, 0, TiffLeSignature)) { return "image/tiff"; }
        if (MatchesAt(buffer, 0, TiffBeSignature)) { return "image/tiff"; }
        if (MatchesAt(buffer, 0, ZipSignature)) { return "application/zip"; }

        // DICOM: magic bytes start at byte offset 128 (requires at least 132 bytes).
        if (buffer.Length >= 132 && MatchesAt(buffer, 128, DicomSignature)) { return "application/dicom"; }

        return null;
    }

    private static bool MatchesAt(ReadOnlySpan<byte> buffer, int offset, byte[] signature)
    {
        if (buffer.Length < offset + signature.Length)
        {
            return false;
        }

        return buffer.Slice(offset, signature.Length).SequenceEqual(signature);
    }
}
