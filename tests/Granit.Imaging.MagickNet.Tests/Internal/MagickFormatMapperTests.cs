using Granit.Imaging.Exceptions;
using Granit.Imaging.MagickNet.Internal;
using ImageMagick;
using Shouldly;
using Xunit;

namespace Granit.Imaging.MagickNet.Tests.Internal;

public sealed class MagickFormatMapperTests
{
    // ── ToMagickFormat ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ImageFormat.Jpeg, MagickFormat.Jpeg)]
    [InlineData(ImageFormat.Png, MagickFormat.Png)]
    [InlineData(ImageFormat.WebP, MagickFormat.WebP)]
    [InlineData(ImageFormat.Avif, MagickFormat.Avif)]
    [InlineData(ImageFormat.Gif, MagickFormat.Gif)]
    [InlineData(ImageFormat.Bmp, MagickFormat.Bmp)]
    [InlineData(ImageFormat.Tiff, MagickFormat.Tiff)]
    public void ToMagickFormat_MapsCorrectly(ImageFormat input, MagickFormat expected) =>
        MagickFormatMapper.ToMagickFormat(input).ShouldBe(expected);

    [Fact]
    public void ToMagickFormat_UnsupportedFormat_Throws() =>
        Should.Throw<UnsupportedImageFormatException>(
            () => MagickFormatMapper.ToMagickFormat((ImageFormat)999));

    // ── FromMagickFormat — JPEG variants ────────────────────────────────────

    [Theory]
    [InlineData(MagickFormat.Jpeg)]
    [InlineData(MagickFormat.Jpg)]
    [InlineData(MagickFormat.Pjpeg)]
    public void FromMagickFormat_JpegVariants_ReturnJpeg(MagickFormat input) =>
        MagickFormatMapper.FromMagickFormat(input).ShouldBe(ImageFormat.Jpeg);

    // ── FromMagickFormat — PNG variants ──────────────────────────────────────

    [Theory]
    [InlineData(MagickFormat.Png)]
    [InlineData(MagickFormat.Png24)]
    [InlineData(MagickFormat.Png32)]
    [InlineData(MagickFormat.Png48)]
    [InlineData(MagickFormat.Png64)]
    [InlineData(MagickFormat.Png8)]
    [InlineData(MagickFormat.Png00)]
    public void FromMagickFormat_PngVariants_ReturnPng(MagickFormat input) =>
        MagickFormatMapper.FromMagickFormat(input).ShouldBe(ImageFormat.Png);

    // ── FromMagickFormat — other formats ────────────────────────────────────

    [Fact]
    public void FromMagickFormat_WebP_ReturnsWebP() =>
        MagickFormatMapper.FromMagickFormat(MagickFormat.WebP).ShouldBe(ImageFormat.WebP);

    [Fact]
    public void FromMagickFormat_Avif_ReturnsAvif() =>
        MagickFormatMapper.FromMagickFormat(MagickFormat.Avif).ShouldBe(ImageFormat.Avif);

    [Theory]
    [InlineData(MagickFormat.Gif)]
    [InlineData(MagickFormat.Gif87)]
    public void FromMagickFormat_GifVariants_ReturnGif(MagickFormat input) =>
        MagickFormatMapper.FromMagickFormat(input).ShouldBe(ImageFormat.Gif);

    [Theory]
    [InlineData(MagickFormat.Bmp)]
    [InlineData(MagickFormat.Bmp2)]
    [InlineData(MagickFormat.Bmp3)]
    public void FromMagickFormat_BmpVariants_ReturnBmp(MagickFormat input) =>
        MagickFormatMapper.FromMagickFormat(input).ShouldBe(ImageFormat.Bmp);

    [Theory]
    [InlineData(MagickFormat.Tiff)]
    [InlineData(MagickFormat.Tiff64)]
    public void FromMagickFormat_TiffVariants_ReturnTiff(MagickFormat input) =>
        MagickFormatMapper.FromMagickFormat(input).ShouldBe(ImageFormat.Tiff);

    [Fact]
    public void FromMagickFormat_UnsupportedFormat_Throws() =>
        Should.Throw<UnsupportedImageFormatException>(
            () => MagickFormatMapper.FromMagickFormat(MagickFormat.Svg));

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.WebP)]
    [InlineData(ImageFormat.Avif)]
    [InlineData(ImageFormat.Gif)]
    [InlineData(ImageFormat.Bmp)]
    [InlineData(ImageFormat.Tiff)]
    public void RoundTrip_ImageFormat_ToMagick_AndBack(ImageFormat format)
    {
        MagickFormat magick = MagickFormatMapper.ToMagickFormat(format);
        ImageFormat roundTripped = MagickFormatMapper.FromMagickFormat(magick);

        roundTripped.ShouldBe(format);
    }
}
