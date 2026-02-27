using Granit.Privacy.DataExport;
using Granit.Privacy.DataExport.Internal;
using Granit.Privacy.LegalAgreements;
using Granit.Privacy.LegalAgreements.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Privacy;

/// <summary>
/// Builder for configuring the Granit.Privacy module.
/// Used within <c>AddGranitPrivacy()</c> to register data providers and legal documents.
/// </summary>
public sealed class GranitPrivacyBuilder(IServiceCollection services)
{
    /// <summary>The underlying service collection.</summary>
    internal IServiceCollection Services { get; } = services;

    /// <summary>Data provider names to register at startup.</summary>
    internal List<string> DataProviderNames { get; } = [];

    /// <summary>Legal document definitions to register at startup.</summary>
    internal List<LegalDocumentDefinition> LegalDocuments { get; } = [];

    /// <summary>
    /// Registers a data provider that participates in GDPR export and deletion.
    /// </summary>
    public GranitPrivacyBuilder RegisterDataProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        DataProviderNames.Add(providerName);
        return this;
    }

    /// <summary>
    /// Registers a legal document for consent versioning.
    /// </summary>
    public GranitPrivacyBuilder RegisterDocument(string documentId, string currentVersion, string displayName)
    {
        LegalDocuments.Add(new LegalDocumentDefinition(documentId, currentVersion, displayName));
        return this;
    }

    /// <summary>
    /// Registers the legal agreement store implementation (provided by the application).
    /// </summary>
    public GranitPrivacyBuilder UseLegalAgreementStore<TStore>()
        where TStore : class, ILegalAgreementStore
    {
        Services.AddScoped<ILegalAgreementStore, TStore>();
        return this;
    }
}
