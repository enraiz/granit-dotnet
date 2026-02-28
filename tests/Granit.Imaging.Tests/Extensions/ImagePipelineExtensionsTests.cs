using Granit.Imaging.Extensions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Imaging.Tests.Extensions;

public sealed class ImagePipelineExtensionsTests
{
    private readonly IImagePipeline _pipeline = Substitute.For<IImagePipeline>();
    private readonly ImageResult _expectedResult = new(
        new byte[] { 0x01, 0x02 },
        ImageFormat.WebP,
        Width: 100,
        Height: 100);

    public ImagePipelineExtensionsTests()
    {
        // ConvertTo returns the same pipeline for chaining
        _pipeline.ConvertTo(Arg.Any<ImageFormat>()).Returns(_pipeline);
        _pipeline.ToResultAsync(Arg.Any<CancellationToken>()).Returns(_expectedResult);
    }

    [Fact]
    public async Task SaveAsJpegAsync_CallsConvertToJpegThenToResult()
    {
        // Act
        ImageResult result = await _pipeline.SaveAsJpegAsync(TestContext.Current.CancellationToken);

        // Assert
        _pipeline.Received(1).ConvertTo(ImageFormat.Jpeg);
        await _pipeline.Received(1).ToResultAsync(TestContext.Current.CancellationToken);
        result.ShouldBe(_expectedResult);
    }

    [Fact]
    public async Task SaveAsPngAsync_CallsConvertToPngThenToResult()
    {
        // Act
        ImageResult result = await _pipeline.SaveAsPngAsync(TestContext.Current.CancellationToken);

        // Assert
        _pipeline.Received(1).ConvertTo(ImageFormat.Png);
        await _pipeline.Received(1).ToResultAsync(TestContext.Current.CancellationToken);
        result.ShouldBe(_expectedResult);
    }

    [Fact]
    public async Task SaveAsWebPAsync_CallsConvertToWebPThenToResult()
    {
        // Act
        ImageResult result = await _pipeline.SaveAsWebPAsync(TestContext.Current.CancellationToken);

        // Assert
        _pipeline.Received(1).ConvertTo(ImageFormat.WebP);
        await _pipeline.Received(1).ToResultAsync(TestContext.Current.CancellationToken);
        result.ShouldBe(_expectedResult);
    }

    [Fact]
    public async Task SaveAsAvifAsync_CallsConvertToAvifThenToResult()
    {
        // Act
        ImageResult result = await _pipeline.SaveAsAvifAsync(TestContext.Current.CancellationToken);

        // Assert
        _pipeline.Received(1).ConvertTo(ImageFormat.Avif);
        await _pipeline.Received(1).ToResultAsync(TestContext.Current.CancellationToken);
        result.ShouldBe(_expectedResult);
    }

    [Fact]
    public async Task SaveAsWebPAsync_PropagatesCancellationToken()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        // Act
        await _pipeline.SaveAsWebPAsync(token);

        // Assert
        await _pipeline.Received(1).ToResultAsync(token);
    }
}
