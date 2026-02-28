namespace Granit.Imaging;

/// <summary>
/// Entry point for image processing. Injected via dependency injection.
/// </summary>
/// <remarks>
/// Use <see cref="Load(Stream)"/> or <see cref="Load(ReadOnlyMemory{byte})"/> to create
/// an <see cref="IImagePipeline"/> that provides a fluent API for image transformations.
/// <para>
/// The returned pipeline must be disposed after use to release native resources:
/// <code>
/// await using IImagePipeline pipeline = imageProcessor.Load(stream);
/// ImageResult result = await pipeline
///     .Resize(800, 600, ResizeMode.Crop)
///     .Compress(quality: 75)
///     .SaveAsWebPAsync();
/// </code>
/// </para>
/// </remarks>
public interface IImageProcessor
{
    /// <summary>
    /// Loads an image from a <see cref="Stream"/> and returns a processing pipeline.
    /// </summary>
    /// <param name="source">The stream containing the image data.</param>
    /// <returns>A fluent pipeline for chaining image operations.</returns>
    IImagePipeline Load(Stream source);

    /// <summary>
    /// Loads an image from a byte buffer and returns a processing pipeline.
    /// </summary>
    /// <param name="source">The byte buffer containing the image data.</param>
    /// <returns>A fluent pipeline for chaining image operations.</returns>
    IImagePipeline Load(ReadOnlyMemory<byte> source);
}
