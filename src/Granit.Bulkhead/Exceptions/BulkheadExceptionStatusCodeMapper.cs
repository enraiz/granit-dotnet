using Granit.ExceptionHandling;
using Microsoft.AspNetCore.Http;

namespace Granit.Bulkhead.Exceptions;

/// <summary>
/// Maps <see cref="BulkheadRejectedException"/> to HTTP 503 Service Unavailable.
/// </summary>
internal sealed class BulkheadExceptionStatusCodeMapper : IExceptionStatusCodeMapper
{
    /// <inheritdoc/>
    public int? TryGetStatusCode(Exception exception) =>
        exception is BulkheadRejectedException ? StatusCodes.Status503ServiceUnavailable : null;
}
