namespace Granit.Privacy.LegalAgreements;

/// <summary>
/// Write-only persistence abstraction for legal agreements. Implemented by the application (EF Core, etc.).
/// </summary>
public interface ILegalAgreementStoreWriter
{
    /// <summary>Records a new legal agreement (append-only).</summary>
    Task RecordAsync(LegalAgreementBase agreement, CancellationToken ct = default);
}
