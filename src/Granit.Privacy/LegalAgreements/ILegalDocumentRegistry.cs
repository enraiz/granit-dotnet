namespace Granit.Privacy.LegalAgreements;

/// <summary>
/// Registry of legal documents declared at startup.
/// </summary>
public interface ILegalDocumentRegistry
{
    /// <summary>Returns the definition for the given document ID, or <c>null</c> if not registered.</summary>
    LegalDocumentDefinition? GetDefinition(string documentId);

    /// <summary>Returns all registered document definitions.</summary>
    IReadOnlyList<LegalDocumentDefinition> GetAll();
}
