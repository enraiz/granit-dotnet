namespace Granit.Imaging.Extensions;

/// <summary>
/// Convenience extension methods for common image output formats.
/// </summary>
public static class ImagePipelineExtensions
{
    /// <summary>
    /// Converts the image to JPEG and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed image as JPEG.</returns>
    public static Task<ImageResult> SaveAsJpegAsync(
        this IImagePipeline pipeline, CancellationToken cancellationToken = default) =>
        pipeline.ConvertTo(ImageFormat.Jpeg).ToResultAsync(cancellationToken);

    /// <summary>
    /// Converts the image to PNG and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed image as PNG.</returns>
    public static Task<ImageResult> SaveAsPngAsync(
        this IImagePipeline pipeline, CancellationToken cancellationToken = default) =>
        pipeline.ConvertTo(ImageFormat.Png).ToResultAsync(cancellationToken);

    /// <summary>
    /// Converts the image to WebP and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed image as WebP.</returns>
    public static Task<ImageResult> SaveAsWebPAsync(
        this IImagePipeline pipeline, CancellationToken cancellationToken = default) =>
        pipeline.ConvertTo(ImageFormat.WebP).ToResultAsync(cancellationToken);

    /// <summary>
    /// Converts the image to AVIF and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed image as AVIF.</returns>
    public static Task<ImageResult> SaveAsAvifAsync(
        this IImagePipeline pipeline, CancellationToken cancellationToken = default) =>
        pipeline.ConvertTo(ImageFormat.Avif).ToResultAsync(cancellationToken);
}
