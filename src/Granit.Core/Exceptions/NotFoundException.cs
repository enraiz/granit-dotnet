namespace Granit.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested resource does not exist.
/// Maps to <c>404 Not Found</c>.
/// </summary>
/// <remarks>
/// Prefer the more specific <see cref="EntityNotFoundException"/> for domain aggregate lookups.
/// Use this base class for non-entity resources (files, blobs, external references)
/// that require a 404 response without leaking domain schema information.
/// </remarks>
public class NotFoundException : Exception, IUserFriendlyException
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="message">Human-readable message safe for client display.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public NotFoundException(string? message = null, Exception? innerException = null)
        : base(message ?? "The requested resource was not found.", innerException)
    {
    }
}
