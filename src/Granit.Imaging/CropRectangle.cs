namespace Granit.Imaging;

/// <summary>
/// Defines a rectangular region for cropping an image.
/// </summary>
/// <param name="X">Horizontal offset from the left edge in pixels.</param>
/// <param name="Y">Vertical offset from the top edge in pixels.</param>
/// <param name="Width">Width of the crop region in pixels.</param>
/// <param name="Height">Height of the crop region in pixels.</param>
public readonly record struct CropRectangle(int X, int Y, int Width, int Height);
