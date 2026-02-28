namespace Granit.Imaging;

/// <summary>
/// Supported image formats for processing and output.
/// </summary>
public enum ImageFormat
{
    /// <summary>JPEG format — lossy compression, no transparency.</summary>
    Jpeg = 0,

    /// <summary>PNG format — lossless compression, supports transparency.</summary>
    Png = 1,

    /// <summary>WebP format — modern lossy/lossless, smaller than JPEG at equivalent quality.</summary>
    WebP = 2,

    /// <summary>AVIF format — next-gen lossy/lossless based on AV1, best compression ratio.</summary>
    Avif = 3,

    /// <summary>GIF format — limited to 256 colors, supports animation.</summary>
    Gif = 4,

    /// <summary>BMP format — uncompressed bitmap.</summary>
    Bmp = 5,

    /// <summary>TIFF format — lossless, used in print and medical imaging.</summary>
    Tiff = 6,
}
