using Granit.Privacy.DataExport;
using Granit.Privacy.DataExport.Internal;
using Granit.Privacy.LegalAgreements;
using Granit.Privacy.LegalAgreements.Internal;
using Granit.Privacy.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Privacy.Extensions;

/// <summary>
/// Extensions for registering Granit.Privacy module services.
/// </summary>
public static class PrivacyServiceCollectionExtensions
{
    /// <summary>
    /// Adds Granit.Privacy services (IDataProviderRegistry, ILegalDocumentRegistry,
    /// ILegalAgreementChecker) and registers data providers and legal documents
    /// declared in the builder.
    /// </summary>
    public static IServiceCollection AddGranitPrivacy(
        this IServiceCollection services,
        Action<GranitPrivacyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<GranitPrivacyOptions>()
            .BindConfiguration(GranitPrivacyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        DataProviderRegistry dataProviderRegistry = new();
        LegalDocumentRegistry legalDocumentRegistry = new();

        GranitPrivacyBuilder builder = new(services);
        configure(builder);

        foreach (string providerName in builder.DataProviderNames)
        {
            dataProviderRegistry.Register(providerName);
        }

        foreach (LegalDocumentDefinition document in builder.LegalDocuments)
        {
            legalDocumentRegistry.Register(document);
        }

        services.TryAddSingleton<IDataProviderRegistry>(dataProviderRegistry);
        services.TryAddSingleton<ILegalDocumentRegistry>(legalDocumentRegistry);
        services.TryAddScoped<ILegalAgreementChecker, LegalAgreementChecker>();

        return services;
    }
}
