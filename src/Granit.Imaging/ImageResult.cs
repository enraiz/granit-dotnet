namespace Granit.Imaging;

/// <summary>
/// The result of an image processing pipeline.
/// </summary>
/// <param name="Content">The raw binary content of the processed image.</param>
/// <param name="Format">The output format of the image.</param>
/// <param name="Width">The width of the processed image in pixels.</param>
/// <param name="Height">The height of the processed image in pixels.</param>
/// <param name="FileName">Optional suggested file name (without path) for download or storage.</param>
public sealed record ImageResult(
    ReadOnlyMemory<byte> Content,
    ImageFormat Format,
    int Width,
    int Height,
    string? FileName = null);
