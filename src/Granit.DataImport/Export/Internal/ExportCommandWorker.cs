using System.Threading.Channels;
using Granit.DataImport.Export.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Granit.DataImport.Export.Internal;

/// <summary>
/// Background service that reads <see cref="ExecuteExportCommand"/> from the channel
/// and executes them via <see cref="IExportOrchestrator"/> in a scoped service context.
/// </summary>
/// <remarks>
/// <para>
/// <b>Graceful shutdown</b>: when the host is stopping, <c>stoppingToken</c>
/// is cancelled. The worker finishes the current export before exiting.
/// </para>
/// <para>
/// Each command gets its own DI scope to properly resolve scoped services
/// (DbContext, stores, etc.).
/// </para>
/// </remarks>
internal sealed partial class ExportCommandWorker(
    Channel<ExecuteExportCommand> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ExportCommandWorker> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ExecuteExportCommand command in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                IExportOrchestrator orchestrator =
                    scope.ServiceProvider.GetRequiredService<IExportOrchestrator>();
                await orchestrator.ExecuteAsync(command.ExportJobId, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogExecutionFailed(command.ExportJobId, ex);
            }
        }
    }

    [LoggerMessage(1, LogLevel.Error, "Export job {ExportJobId} execution failed in background worker")]
    private partial void LogExecutionFailed(Guid exportJobId, Exception ex);
}
