using System.Reflection;
using System.Threading.Channels;
using Cronos;
using Granit.BackgroundJobs.Abstractions;
using Granit.BackgroundJobs.Domain;
using Granit.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Background service that reads <see cref="BackgroundJobEnvelope"/> from the channel
/// and invokes job handlers in a DI scope. Replicates the scheduling middleware logic
/// for the in-process dispatch path.
/// </summary>
internal sealed partial class BackgroundJobWorker(
    Channel<BackgroundJobEnvelope> channel,
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<BackgroundJobWorker> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BackgroundJobEnvelope envelope in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(envelope, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogExecutionFailed(envelope.Message.GetType().Name, ex);
            }
        }
    }

    private async Task ProcessAsync(BackgroundJobEnvelope envelope, CancellationToken cancellationToken)
    {
        RecurringJobAttribute? attr = envelope.Message.GetType()
            .GetCustomAttribute<RecurringJobAttribute>();

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        if (attr is not null)
        {
            IBackgroundJobStoreWriter storeWriter =
                scope.ServiceProvider.GetRequiredService<IBackgroundJobStoreWriter>();

            // Record execution start + triggered-by header (ISO 27001 audit).
            await storeWriter.RecordExecutionStartAsync(attr.Name, clock.Now, cancellationToken)
                .ConfigureAwait(false);

            if (envelope.Headers?.TryGetValue(BackgroundJobHeaders.TriggeredBy, out string? triggeredBy) == true
                && !string.IsNullOrEmpty(triggeredBy))
            {
                await storeWriter.SetTriggeredByAsync(attr.Name, triggeredBy, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        try
        {
            // Invoke the handler by resolving the Wolverine-convention HandleAsync method.
            await InvokeHandlerAsync(scope.ServiceProvider, envelope.Message, cancellationToken)
                .ConfigureAwait(false);

            // On success: reschedule if recurring.
            if (attr is not null)
            {
                await RescheduleAsync(scope.ServiceProvider, attr, envelope.Message.GetType(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (attr is not null)
            {
                IBackgroundJobStoreWriter storeWriter =
                    scope.ServiceProvider.GetRequiredService<IBackgroundJobStoreWriter>();
                await storeWriter.RecordExecutionFailureAsync(attr.Name, ex.Message, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>
    /// Resolves the handler for the message type and invokes <c>HandleAsync(message, ct)</c>.
    /// Wolverine convention: the handler type name = <c>{MessageTypeName}Handler</c>.
    /// </summary>
    private static async Task InvokeHandlerAsync(
        IServiceProvider services,
        object message,
        CancellationToken cancellationToken)
    {
        Type messageType = message.GetType();

        // Look for a HandleAsync(TMessage, CancellationToken) method on any registered service
        // whose type name matches the convention "{MessageTypeName}Handler".
        string expectedHandlerName = $"{messageType.Name}Handler";

        // Try to resolve from DI by scanning registered services.
        // Fallback: try the assembly of the message type.
        Type? handlerType = messageType.Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == expectedHandlerName && !t.IsAbstract);

        if (handlerType is null)
        {
            throw new InvalidOperationException(
                $"No handler '{expectedHandlerName}' found in assembly '{messageType.Assembly.GetName().Name}' " +
                $"for message type '{messageType.Name}'.");
        }

        object handler = services.GetRequiredService(handlerType);

        MethodInfo? method = handlerType.GetMethod("HandleAsync",
            [messageType, typeof(CancellationToken)]);

        if (method is null)
        {
            throw new InvalidOperationException(
                $"Handler '{handlerType.Name}' does not have a HandleAsync({messageType.Name}, CancellationToken) method.");
        }

        var task = (Task)method.Invoke(handler, [message, cancellationToken])!;
        await task.ConfigureAwait(false);
    }

    private async Task RescheduleAsync(
        IServiceProvider services,
        RecurringJobAttribute attr,
        Type messageType,
        CancellationToken cancellationToken)
    {
        IBackgroundJobStoreReader storeReader =
            services.GetRequiredService<IBackgroundJobStoreReader>();
        IBackgroundJobStoreWriter storeWriter =
            services.GetRequiredService<IBackgroundJobStoreWriter>();
        IBackgroundJobDispatcher dispatcher =
            services.GetRequiredService<IBackgroundJobDispatcher>();

        BackgroundJobDefinition? job = await storeReader.FindAsync(attr.Name, cancellationToken)
            .ConfigureAwait(false);

        if (job is not { IsEnabled: true })
        {
            return;
        }

        DateTimeOffset? next = ComputeNext(job.CronExpression);
        if (next is null)
        {
            LogNoCronOccurrence(attr.Name, job.CronExpression);
            return;
        }

        object nextMessage = Activator.CreateInstance(messageType)!;
        await dispatcher.ScheduleAsync(nextMessage, next.Value, cancellationToken).ConfigureAwait(false);
        await storeWriter.RecordNextExecutionAsync(job.JobName, next.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    private DateTimeOffset? ComputeNext(string cronExpression)
    {
        try
        {
            CronExpression cron;
            try
            {
                cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            }
            catch (CronFormatException)
            {
                cron = CronExpression.Parse(cronExpression);
            }

            return cron.GetNextOccurrence(clock.Now, TimeZoneInfo.Utc);
        }
        catch (CronFormatException)
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Background job execution failed for message type '{MessageTypeName}'")]
    private partial void LogExecutionFailed(string messageTypeName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "RecurringJob '{JobName}': cron expression '{Cron}' produced no next occurrence")]
    private partial void LogNoCronOccurrence(string jobName, string cron);
}
