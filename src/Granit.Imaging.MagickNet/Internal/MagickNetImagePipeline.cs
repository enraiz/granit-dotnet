using ImageMagick;

namespace Granit.Imaging.MagickNet.Internal;

/// <summary>
/// Magick.NET implementation of <see cref="IImagePipeline"/>.
/// Each transformation mutates the underlying <see cref="MagickImage"/> immediately (eager execution).
/// </summary>
internal sealed class MagickNetImagePipeline : IImagePipeline
{
    private readonly MagickImage _image;
    private int? _quality;
    private ImageFormat? _targetFormat;

    internal MagickNetImagePipeline(MagickImage image)
    {
        _image = image;
        SourceSize = new ImageSize((int)_image.Width, (int)_image.Height);
        SourceFormat = MagickFormatMapper.FromMagickFormat(_image.Format);
    }

    /// <inheritdoc/>
    public ImageSize SourceSize { get; }

    /// <inheritdoc/>
    public ImageFormat SourceFormat { get; }

    /// <inheritdoc/>
    public IImagePipeline Resize(int width, int height, ResizeMode mode = ResizeMode.Max)
    {
        MagickGeometry geometry = new((uint)width, (uint)height);

        switch (mode)
        {
            case ResizeMode.Max:
                geometry.IgnoreAspectRatio = false;
                _image.Resize(geometry);
                break;

            case ResizeMode.Crop:
                geometry.IgnoreAspectRatio = false;
                geometry.FillArea = true;
                _image.Resize(geometry);
                _image.Crop((uint)width, (uint)height, Gravity.Center);
                _image.ResetPage();
                break;

            case ResizeMode.Pad:
                geometry.IgnoreAspectRatio = false;
                _image.Resize(geometry);
                _image.Extent((uint)width, (uint)height, Gravity.Center, MagickColors.Transparent);
                break;

            case ResizeMode.Stretch:
                geometry.IgnoreAspectRatio = true;
                _image.Resize(geometry);
                break;

            case ResizeMode.Min:
                geometry.IgnoreAspectRatio = false;
                geometry.FillArea = true;
                _image.Resize(geometry);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown resize mode.");
        }

        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline Crop(CropRectangle rectangle)
    {
        _image.Crop(new MagickGeometry(rectangle.X, rectangle.Y, (uint)rectangle.Width, (uint)rectangle.Height));
        _image.ResetPage();
        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline Compress(int quality)
    {
        _quality = quality;
        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline ConvertTo(ImageFormat format)
    {
        _targetFormat = format;
        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline Watermark(
        ReadOnlyMemory<byte> watermark,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        float opacity = 0.5f)
    {
        using MagickImage overlay = new(watermark.Span);
        ApplyWatermark(overlay, position, opacity);
        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline Watermark(
        Stream watermark,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        float opacity = 0.5f)
    {
        using MagickImage overlay = new(watermark);
        ApplyWatermark(overlay, position, opacity);
        return this;
    }

    /// <inheritdoc/>
    public IImagePipeline StripMetadata()
    {
        _image.Strip();
        return this;
    }

    /// <inheritdoc/>
    public Task<ImageResult> ToResultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplyOutputSettings();

        using MemoryStream ms = new();
        _image.Write(ms, MagickFormatMapper.ToMagickFormat(_targetFormat ?? SourceFormat));

        ImageResult result = new(
            ms.ToArray(),
            _targetFormat ?? SourceFormat,
            (int)_image.Width,
            (int)_image.Height);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task SaveToStreamAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplyOutputSettings();

        _image.Write(destination, MagickFormatMapper.ToMagickFormat(_targetFormat ?? SourceFormat));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _image.Dispose();
        return ValueTask.CompletedTask;
    }

    private void ApplyOutputSettings()
    {
        if (_quality.HasValue)
        {
            _image.Quality = (uint)_quality.Value;
        }
    }

    private void ApplyWatermark(MagickImage overlay, WatermarkPosition position, float opacity)
    {
        overlay.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, opacity);
        _image.Composite(overlay, ToGravity(position), CompositeOperator.Over);
    }

    private static Gravity ToGravity(WatermarkPosition position) => position switch
    {
        WatermarkPosition.Center => Gravity.Center,
        WatermarkPosition.TopLeft => Gravity.Northwest,
        WatermarkPosition.TopRight => Gravity.Northeast,
        WatermarkPosition.BottomLeft => Gravity.Southwest,
        WatermarkPosition.BottomRight => Gravity.Southeast,
        _ => Gravity.Center,
    };
}
