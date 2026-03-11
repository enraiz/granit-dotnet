using Granit.Privacy.LegalAgreements.Domain;

namespace Granit.Privacy.LegalAgreements;

/// <summary>
/// Checks whether a user has accepted the latest version of a legal document.
/// </summary>
public interface ILegalAgreementChecker
{
    /// <summary>
    /// Returns <c>true</c> if the user has accepted the current version of the specified document.
    /// </summary>
    Task<bool> HasAcceptedLatestAsync(Guid userId, string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full consent history for a user, ordered by date (most recent first).
    /// </summary>
    Task<IReadOnlyList<LegalAgreementBase>> GetUserAgreementsAsync(Guid userId, CancellationToken cancellationToken = default);
}
