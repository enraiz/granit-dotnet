using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Granit.DocumentGeneration.Pdf.Internal;

/// <summary>
/// Manages the lifecycle of the headless Chromium browser instance.
/// Starts the browser on application startup and disposes it on shutdown.
/// </summary>
internal sealed class ChromiumLifetimeService(
    IOptions<PdfRenderOptions> options,
    ILogger<ChromiumLifetimeService> logger) : IHostedService, IAsyncDisposable
{
    private IBrowser? _browser;
    private readonly SemaphoreSlim _pageSemaphore = new(options.Value.MaxConcurrentPages);

    /// <summary>
    /// Gets the browser instance. Throws if the service has not been started.
    /// </summary>
    internal IBrowser Browser => _browser ?? throw new InvalidOperationException(
        "Chromium browser is not available. Ensure ChromiumLifetimeService has been started.");

    /// <summary>
    /// Gets the semaphore that limits concurrent page usage.
    /// </summary>
    internal SemaphoreSlim PageSemaphore => _pageSemaphore;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        PdfRenderOptions opts = options.Value;

        if (string.IsNullOrEmpty(opts.ChromiumExecutablePath))
        {
            logger.LogInformation("Downloading Chromium browser for PDF rendering...");
            BrowserFetcher fetcher = new();
            await fetcher.DownloadAsync().ConfigureAwait(false);
        }

        LaunchOptions launchOptions = new()
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"],
        };

        if (!string.IsNullOrEmpty(opts.ChromiumExecutablePath))
        {
            launchOptions.ExecutablePath = opts.ChromiumExecutablePath;
        }

        logger.LogInformation("Starting headless Chromium for PDF rendering...");
        _browser = await Puppeteer.LaunchAsync(launchOptions).ConfigureAwait(false);
        logger.LogInformation("Headless Chromium started successfully");
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            logger.LogInformation("Shutting down headless Chromium...");
            await _browser.DisposeAsync().ConfigureAwait(false);
            _browser = null;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync().ConfigureAwait(false);
            _browser = null;
        }

        _pageSemaphore.Dispose();
    }
}
