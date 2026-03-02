namespace Granit.Privacy.LegalAgreements.Internal;

/// <summary>
/// Checks consent by comparing the stored version with the current version in the registry.
/// </summary>
internal sealed class LegalAgreementChecker(
    ILegalDocumentRegistry documentRegistry,
    ILegalAgreementStore store) : ILegalAgreementChecker
{
    /// <inheritdoc/>
    public async Task<bool> HasAcceptedLatestAsync(Guid userId, string documentId, CancellationToken ct = default)
    {
        LegalDocumentDefinition? definition = documentRegistry.GetDefinition(documentId);
        if (definition is null)
        {
            return false;
        }

        LegalAgreementBase? latest = await store.FindLatestAsync(userId, documentId, ct).ConfigureAwait(false);
        if (latest is null)
        {
            return false;
        }

        return string.Equals(latest.Version, definition.CurrentVersion, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LegalAgreementBase>> GetUserAgreementsAsync(Guid userId, CancellationToken ct = default) =>
        store.FindAllByUserAsync(userId, ct);
}
