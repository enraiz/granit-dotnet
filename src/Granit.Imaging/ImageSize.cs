namespace Granit.Imaging;

/// <summary>
/// Represents the width and height of an image in pixels.
/// </summary>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
public readonly record struct ImageSize(int Width, int Height);
