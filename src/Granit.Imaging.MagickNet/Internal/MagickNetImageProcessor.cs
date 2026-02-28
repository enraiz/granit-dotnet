using ImageMagick;

namespace Granit.Imaging.MagickNet.Internal;

/// <summary>
/// Magick.NET implementation of <see cref="IImageProcessor"/>.
/// Stateless singleton that creates <see cref="MagickNetImagePipeline"/> instances.
/// </summary>
internal sealed class MagickNetImageProcessor : IImageProcessor
{
    /// <inheritdoc/>
    public IImagePipeline Load(Stream source)
    {
        MagickImage image = new(source);
        return new MagickNetImagePipeline(image);
    }

    /// <inheritdoc/>
    public IImagePipeline Load(ReadOnlyMemory<byte> source)
    {
        MagickImage image = new(source.Span);
        return new MagickNetImagePipeline(image);
    }
}
