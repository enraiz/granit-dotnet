using Granit.DocumentGeneration.Pdf.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PuppeteerSharp;
using Shouldly;
using Xunit;

namespace Granit.DocumentGeneration.Pdf.Tests;

public sealed class ChromiumLifetimeServiceTests
{
    [Fact]
    public void Browser_WhenNotStarted_ThrowsInvalidOperationException()
    {
        ChromiumLifetimeService service = CreateService();

        Func<object> act = () => service.Browser;

        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("Chromium browser is not available");
    }

    [Fact]
    public void PageSemaphore_DefaultConcurrency_Is4()
    {
        ChromiumLifetimeService service = CreateService();

        service.PageSemaphore.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void PageSemaphore_CustomConcurrency_MatchesOption()
    {
        PdfRenderOptions options = new() { MaxConcurrentPages = 8 };
        ChromiumLifetimeService service = CreateService(options);

        service.PageSemaphore.CurrentCount.ShouldBe(8);
    }

    [Fact]
    public async Task StopAsync_WhenBrowserNotStarted_DoesNotThrow()
    {
        ChromiumLifetimeService service = CreateService();

        await service.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StopAsync_WithBrowser_DisposesBrowser()
    {
        IBrowser browser = Substitute.For<IBrowser>();
        ChromiumLifetimeService service = CreateServiceWithBrowser(browser);

        await service.StopAsync(TestContext.Current.CancellationToken);

        await browser.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_WithBrowser_SetsBrowserToNull()
    {
        IBrowser browser = Substitute.For<IBrowser>();
        ChromiumLifetimeService service = CreateServiceWithBrowser(browser);

        await service.StopAsync(TestContext.Current.CancellationToken);

        Func<object> act = () => service.Browser;
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public async Task DisposeAsync_WhenBrowserNotStarted_DoesNotThrow()
    {
        ChromiumLifetimeService service = CreateService();

        await service.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithBrowser_DisposesBrowser()
    {
        IBrowser browser = Substitute.For<IBrowser>();
        ChromiumLifetimeService service = CreateServiceWithBrowser(browser);

        await service.DisposeAsync();

        await browser.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_DisposesPageSemaphore()
    {
        ChromiumLifetimeService service = CreateService();

        await service.DisposeAsync();

        Func<Task> act = () => service.PageSemaphore.WaitAsync();
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }

    private static ChromiumLifetimeService CreateService(PdfRenderOptions? options = null) =>
        new(
            Options.Create(options ?? new PdfRenderOptions()),
            NullLogger<ChromiumLifetimeService>.Instance);

    private static ChromiumLifetimeService CreateServiceWithBrowser(IBrowser browser)
    {
        PdfRenderOptions options = new();
        ChromiumLifetimeService service = new(
            Options.Create(options),
            NullLogger<ChromiumLifetimeService>.Instance);

        System.Reflection.FieldInfo? browserField = typeof(ChromiumLifetimeService)
            .GetField("_browser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        browserField!.SetValue(service, browser);

        return service;
    }
}
