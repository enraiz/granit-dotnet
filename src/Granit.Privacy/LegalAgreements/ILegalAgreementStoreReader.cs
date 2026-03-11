using Granit.Privacy.LegalAgreements.Domain;

namespace Granit.Privacy.LegalAgreements;

/// <summary>
/// Read-only persistence abstraction for legal agreements. Implemented by the application (EF Core, etc.).
/// </summary>
public interface ILegalAgreementStoreReader
{
    /// <summary>Returns the latest agreement for a user and document, or <c>null</c> if none.</summary>
    Task<LegalAgreementBase?> FindLatestAsync(Guid userId, string documentId, CancellationToken cancellationToken = default);

    /// <summary>Returns all agreements for a user, ordered by date (most recent first).</summary>
    Task<IReadOnlyList<LegalAgreementBase>> FindAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
