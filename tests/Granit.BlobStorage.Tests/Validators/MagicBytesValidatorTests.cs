using FluentAssertions;
using Granit.BlobStorage.Validators;
using Xunit;

namespace Granit.BlobStorage.Tests.Validators;

public sealed class MagicBytesValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

    private static BlobDescriptor MakeDescriptor(string contentType) =>
        BlobDescriptor.Create(
            id: Guid.NewGuid(),
            tenantId: "tenant-abc",
            containerName: "prescriptions",
            objectKey: "tenant-abc/prescriptions/2026/02/some-id",
            originalFileName: "file",
            declaredContentType: contentType,
            maxAllowedBytes: 10_000_000L,
            createdAt: Now);

    private static BlobValidationContext MakeContext(BlobDescriptor descriptor, byte[] bytes) =>
        new()
        {
            Descriptor = descriptor,
            ActualSizeBytes = bytes.Length,
            OpenPartialStreamAsync = (_, _) =>
                Task.FromResult<Stream>(new MemoryStream(bytes)),
        };

    // ── MagicByteDetector unit tests ─────────────────────────────────────────

    [Fact]
    public void Detect_PdfSignature_ReturnsPdf()
    {
        byte[] buffer = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]; // %PDF-1.4

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().Be("application/pdf");
    }

    [Fact]
    public void Detect_JpegSignature_ReturnsJpeg()
    {
        byte[] buffer = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().Be("image/jpeg");
    }

    [Fact]
    public void Detect_PngSignature_ReturnsPng()
    {
        byte[] buffer = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().Be("image/png");
    }

    [Fact]
    public void Detect_DicomSignatureAtOffset128_ReturnsDicom()
    {
        byte[] buffer = new byte[132];
        // Pad to offset 128 then write DICM
        buffer[128] = 0x44; // D
        buffer[129] = 0x49; // I
        buffer[130] = 0x43; // C
        buffer[131] = 0x4D; // M

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().Be("application/dicom");
    }

    [Fact]
    public void Detect_ZipSignature_ReturnsZip()
    {
        byte[] buffer = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00];

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().Be("application/zip");
    }

    [Fact]
    public void Detect_UnknownBytes_ReturnsNull()
    {
        byte[] buffer = [0x00, 0x01, 0x02, 0x03, 0x04];

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().BeNull();
    }

    [Fact]
    public void Detect_DicomBufferTooShort_DoesNotReturnDicom()
    {
        // Buffer shorter than 132 bytes: DICOM should not be detected
        byte[] buffer = new byte[131];
        buffer[128] = 0x44;
        buffer[129] = 0x49;
        buffer[130] = 0x43;

        string? result = MagicByteDetector.Detect(buffer);

        result.Should().BeNull();
    }

    // ── MagicBytesValidator.ValidateAsync ────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_PdfBytesWithPdfDeclared_ReturnsSuccess()
    {
        MagicBytesValidator validator = new();
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
        BlobValidationContext context = MakeContext(MakeDescriptor("application/pdf"), pdfBytes);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.VerifiedContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ValidateAsync_JpegBytesDeclaredAsPdf_ReturnsFailure()
    {
        MagicBytesValidator validator = new();
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        BlobValidationContext context = MakeContext(MakeDescriptor("application/pdf"), jpegBytes);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Contain("image/jpeg");
        result.FailureReason.Should().Contain("application/pdf");
    }

    [Fact]
    public async Task ValidateAsync_UnknownBytesWithAnyDeclaredType_PassesThroughWithDeclaredType()
    {
        MagicBytesValidator validator = new();
        byte[] unknownBytes = new byte[50]; // all zeros — no known signature
        BlobDescriptor descriptor = MakeDescriptor("application/octet-stream");
        BlobValidationContext context = MakeContext(descriptor, unknownBytes);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue("unknown formats pass through to avoid false negatives");
        result.VerifiedContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task ValidateAsync_PngBytesCaseInsensitiveMatch_ReturnsSuccess()
    {
        MagicBytesValidator validator = new();
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        // Declared in uppercase (unusual but should work)
        BlobValidationContext context = MakeContext(MakeDescriptor("IMAGE/PNG"), pngBytes);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Order_Is10() =>
        new MagicBytesValidator().Order.Should().Be(10);
}
