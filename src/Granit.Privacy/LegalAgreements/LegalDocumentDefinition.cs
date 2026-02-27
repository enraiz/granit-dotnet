namespace Granit.Privacy.LegalAgreements;

/// <summary>
/// Immutable definition of a legal document registered at startup.
/// </summary>
/// <param name="DocumentId">Unique identifier (e.g., "privacy-policy").</param>
/// <param name="CurrentVersion">Current version of the document (e.g., "2.1.0").</param>
/// <param name="DisplayName">Human-readable name for audit reports.</param>
public sealed record LegalDocumentDefinition(
    string DocumentId,
    string CurrentVersion,
    string DisplayName);
