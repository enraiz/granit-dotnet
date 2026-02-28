using Granit.Imaging.Exceptions;
using ImageMagick;

namespace Granit.Imaging.MagickNet.Internal;

/// <summary>
/// Bidirectional mapping between <see cref="ImageFormat"/> and <see cref="MagickFormat"/>.
/// </summary>
internal static class MagickFormatMapper
{
    /// <summary>
    /// Converts a Granit <see cref="ImageFormat"/> to the corresponding <see cref="MagickFormat"/>.
    /// </summary>
    internal static MagickFormat ToMagickFormat(ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => MagickFormat.Jpeg,
        ImageFormat.Png => MagickFormat.Png,
        ImageFormat.WebP => MagickFormat.WebP,
        ImageFormat.Avif => MagickFormat.Avif,
        ImageFormat.Gif => MagickFormat.Gif,
        ImageFormat.Bmp => MagickFormat.Bmp,
        ImageFormat.Tiff => MagickFormat.Tiff,
        _ => throw new UnsupportedImageFormatException(format.ToString()),
    };

    /// <summary>
    /// Converts a <see cref="MagickFormat"/> to the corresponding Granit <see cref="ImageFormat"/>.
    /// </summary>
    internal static ImageFormat FromMagickFormat(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg or MagickFormat.Jpg or MagickFormat.Pjpeg => ImageFormat.Jpeg,
        MagickFormat.Png or MagickFormat.Png24 or MagickFormat.Png32
            or MagickFormat.Png48 or MagickFormat.Png64
            or MagickFormat.Png8 or MagickFormat.Png00 => ImageFormat.Png,
        MagickFormat.WebP => ImageFormat.WebP,
        MagickFormat.Avif => ImageFormat.Avif,
        MagickFormat.Gif or MagickFormat.Gif87 => ImageFormat.Gif,
        MagickFormat.Bmp or MagickFormat.Bmp2 or MagickFormat.Bmp3 => ImageFormat.Bmp,
        MagickFormat.Tiff or MagickFormat.Tiff64 => ImageFormat.Tiff,
        _ => throw new UnsupportedImageFormatException(format.ToString()),
    };
}
