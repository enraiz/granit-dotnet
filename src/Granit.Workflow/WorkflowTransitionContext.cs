namespace Granit.Workflow;

/// <summary>
/// Ambient context for attaching a comment/reason to the current workflow transition.
/// Uses <see cref="AsyncLocal{T}"/> for per-async-flow isolation
/// (same pattern as <c>ICurrentTenant</c>).
/// </summary>
/// <remarks>
/// The <c>WorkflowTransitionInterceptor</c> reads <see cref="Current"/> during
/// <c>SaveChanges</c> to populate the <see cref="Domain.WorkflowTransitionRecord.Comment"/>
/// field of the ISO 27001 audit trail.
/// </remarks>
/// <example>
/// <code>
/// using (WorkflowTransitionContext.SetComment("Validated by medical director per ISO 27001 protocol"))
/// {
///     invoice.Status = InvoiceStatus.Approved;
///     await dbContext.SaveChangesAsync(cancellationToken);
/// }
/// </code>
/// </example>
public static class WorkflowTransitionContext
{
    private static readonly AsyncLocal<TransitionInfo?> CurrentValue = new();

    /// <summary>
    /// Current transition info in the async flow. <c>null</c> when no transition is in progress.
    /// </summary>
    public static TransitionInfo? Current => CurrentValue.Value;

    /// <summary>
    /// Sets the transition comment for the current async flow.
    /// Disposing the returned scope restores the previous state.
    /// </summary>
    public static IDisposable SetComment(string? comment)
    {
        TransitionInfo? previous = CurrentValue.Value;
        CurrentValue.Value = new TransitionInfo { Comment = comment };
        return new TransitionScope(previous);
    }

    /// <summary>Holds the transition metadata for the current async flow.</summary>
    public sealed record TransitionInfo
    {
        /// <summary>Optional comment or regulatory justification.</summary>
        public string? Comment { get; init; }
    }

    private sealed class TransitionScope(TransitionInfo? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                CurrentValue.Value = previous;
            }
        }
    }
}
