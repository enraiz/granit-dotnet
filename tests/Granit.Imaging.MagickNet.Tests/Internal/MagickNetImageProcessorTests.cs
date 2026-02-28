using FluentAssertions;
using Granit.Imaging.MagickNet.Internal;
using Xunit;

namespace Granit.Imaging.MagickNet.Tests.Internal;

public sealed class MagickNetImageProcessorTests
{
    private readonly MagickNetImageProcessor _processor = new();

    private static Stream GetTestImageStream() =>
        typeof(MagickNetImageProcessorTests).Assembly
            .GetManifestResourceStream("Granit.Imaging.MagickNet.Tests.TestAssets.test-image.png")!;

    private static ReadOnlyMemory<byte> GetTestImageBytes()
    {
        using Stream stream = GetTestImageStream();
        using MemoryStream ms = new();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Load_FromStream_ReturnsPipelineWithCorrectSourceFormat()
    {
        // Arrange
        using Stream stream = GetTestImageStream();

        // Act
        IImagePipeline pipeline = _processor.Load(stream);

        // Assert
        pipeline.SourceFormat.Should().Be(ImageFormat.Png);
        pipeline.SourceSize.Width.Should().Be(100);
        pipeline.SourceSize.Height.Should().Be(100);
    }

    [Fact]
    public void Load_FromReadOnlyMemory_ReturnsPipelineWithCorrectSourceSize()
    {
        // Arrange
        ReadOnlyMemory<byte> bytes = GetTestImageBytes();

        // Act
        IImagePipeline pipeline = _processor.Load(bytes);

        // Assert
        pipeline.SourceFormat.Should().Be(ImageFormat.Png);
        pipeline.SourceSize.Should().Be(new ImageSize(100, 100));
    }

    [Fact]
    public void Load_FromStream_PipelineIsDisposable()
    {
        // Arrange
        using Stream stream = GetTestImageStream();

        // Act
        IImagePipeline pipeline = _processor.Load(stream);

        // Assert
        Func<Task> act = () => pipeline.DisposeAsync().AsTask();
        act.Should().NotThrowAsync();
    }
}
