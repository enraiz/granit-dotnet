using System.Threading.Channels;
using Granit.DataImport.Messages;
using Granit.DataImport.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Granit.DataImport.Internal;

/// <summary>
/// Background service that reads <see cref="ExecuteImportCommand"/> from the channel
/// and executes them via <see cref="IImportOrchestrator"/> in a scoped service context.
/// </summary>
/// <remarks>
/// <para>
/// <b>Graceful shutdown</b>: when the host is stopping, <c>stoppingToken</c>
/// is cancelled. The worker finishes the current import before exiting.
/// </para>
/// <para>
/// Each command gets its own DI scope to properly resolve scoped services
/// (DbContext, stores, etc.).
/// </para>
/// </remarks>
internal sealed partial class ImportCommandWorker(
    Channel<ExecuteImportCommand> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportCommandWorker> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ExecuteImportCommand command in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                IImportOrchestrator orchestrator =
                    scope.ServiceProvider.GetRequiredService<IImportOrchestrator>();
                await orchestrator.ExecuteAsync(command.ImportJobId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogExecutionFailed(command.ImportJobId, ex);
            }
        }
    }

    [LoggerMessage(1, LogLevel.Error, "Import job {ImportJobId} execution failed in background worker")]
    private partial void LogExecutionFailed(Guid importJobId, Exception ex);
}
