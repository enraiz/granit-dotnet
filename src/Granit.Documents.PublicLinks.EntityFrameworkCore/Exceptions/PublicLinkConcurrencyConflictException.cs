namespace Granit.Documents.PublicLinks.EntityFrameworkCore.Exceptions;

/// <summary>
/// Raised by <c>IDocumentPublicLinkStore</c> implementations when an
/// optimistic concurrency check fails on update — typically two concurrent
/// redemptions racing against the same <c>MaxUses</c> cap.
/// </summary>
/// <remarks>
/// Domain-level wrapper so <c>DocumentPublicLinkService</c> can react without
/// taking a hard dependency on <c>Microsoft.EntityFrameworkCore</c>.
/// </remarks>
internal sealed class PublicLinkConcurrencyConflictException : Exception
{
    public PublicLinkConcurrencyConflictException()
        : base("DocumentPublicLink update lost the optimistic concurrency race.")
    {
    }

    public PublicLinkConcurrencyConflictException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
