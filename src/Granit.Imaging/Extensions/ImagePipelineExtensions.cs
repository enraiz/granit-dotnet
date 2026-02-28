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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed image as JPEG.</returns>
    public static Task<ImageResult> SaveAsJpegAsync(
        this IImagePipeline pipeline, CancellationToken ct = default) =>
        pipeline.ConvertTo(ImageFormat.Jpeg).ToResultAsync(ct);

    /// <summary>
    /// Converts the image to PNG and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed image as PNG.</returns>
    public static Task<ImageResult> SaveAsPngAsync(
        this IImagePipeline pipeline, CancellationToken ct = default) =>
        pipeline.ConvertTo(ImageFormat.Png).ToResultAsync(ct);

    /// <summary>
    /// Converts the image to WebP and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed image as WebP.</returns>
    public static Task<ImageResult> SaveAsWebPAsync(
        this IImagePipeline pipeline, CancellationToken ct = default) =>
        pipeline.ConvertTo(ImageFormat.WebP).ToResultAsync(ct);

    /// <summary>
    /// Converts the image to AVIF and returns the result.
    /// </summary>
    /// <param name="pipeline">The image pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed image as AVIF.</returns>
    public static Task<ImageResult> SaveAsAvifAsync(
        this IImagePipeline pipeline, CancellationToken ct = default) =>
        pipeline.ConvertTo(ImageFormat.Avif).ToResultAsync(ct);
}
