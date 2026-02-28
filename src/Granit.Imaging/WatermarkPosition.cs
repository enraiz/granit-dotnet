namespace Granit.Imaging;

/// <summary>
/// Determines the placement of a watermark overlay on the target image.
/// </summary>
public enum WatermarkPosition
{
    /// <summary>Centered on the image.</summary>
    Center = 0,

    /// <summary>Top-left corner.</summary>
    TopLeft = 1,

    /// <summary>Top-right corner.</summary>
    TopRight = 2,

    /// <summary>Bottom-left corner.</summary>
    BottomLeft = 3,

    /// <summary>Bottom-right corner.</summary>
    BottomRight = 4,
}
