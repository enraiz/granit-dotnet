using Granit.Core.Modularity;
using Granit.Diagnostics;
using Granit.ExceptionHandling;
using Granit.Observability;
using Granit.Persistence;
using Granit.Security;
using Granit.Timing;
using Granit.Validation;
using Shouldly;
using Xunit;

namespace Granit.Bundle.Tests;

/// <summary>
/// Verifies that meta-packages expose their transitive dependencies correctly.
/// If a type is not accessible, the project reference chain is broken.
/// </summary>
public sealed class BundleTransitiveDependencyTests
{
    [Fact]
    public void Essentials_ExposesAllTransitiveDependencies()
    {
        // These types come from 9 different packages — all transitively
        // available through Granit.Bundle.Essentials.
        typeof(GranitModule).Assembly.ShouldNotBeNull();                 // Core
        typeof(IClock).Assembly.ShouldNotBeNull();                       // Timing
        typeof(Guids.IGuidGenerator).Assembly.ShouldNotBeNull();         // Guids
        typeof(ICurrentUserService).Assembly.ShouldNotBeNull();          // Security
        typeof(GranitValidationModule).Assembly.ShouldNotBeNull();       // Validation
        typeof(GranitPersistenceModule).Assembly.ShouldNotBeNull();      // Persistence
        typeof(GranitObservabilityModule).Assembly.ShouldNotBeNull();    // Observability
        typeof(GranitExceptionHandlingModule).Assembly.ShouldNotBeNull(); // ExceptionHandling
        typeof(GranitDiagnosticsModule).Assembly.ShouldNotBeNull();      // Diagnostics
    }

    [Fact]
    public void Api_IncludesEssentialsAndApiModules()
    {
        // Essentials types (transitive through Bundle.Api → Bundle.Essentials)
        typeof(GranitModule).Assembly.ShouldNotBeNull();
        typeof(IClock).Assembly.ShouldNotBeNull();

        // API-specific types
        typeof(ApiVersioning.GranitApiVersioningModule).Assembly.ShouldNotBeNull();
        typeof(ApiDocumentation.GranitApiDocumentationModule).Assembly.ShouldNotBeNull();
        typeof(Cors.GranitCorsModule).Assembly.ShouldNotBeNull();
        typeof(Idempotency.GranitIdempotencyModule).Assembly.ShouldNotBeNull();
        typeof(Localization.GranitLocalizationModule).Assembly.ShouldNotBeNull();
        typeof(Caching.GranitCachingModule).Assembly.ShouldNotBeNull();
    }

    [Fact]
    public void Notifications_ExposesAllNotificationModules()
    {
        typeof(Notifications.GranitNotificationsModule).Assembly.ShouldNotBeNull();
        typeof(Notifications.EntityFrameworkCore.GranitNotificationsEntityFrameworkCoreModule)
            .Assembly.ShouldNotBeNull();
        typeof(Notifications.Email.EmailMessage).Assembly.ShouldNotBeNull();
        typeof(Notifications.SignalR.NotificationHub).Assembly.ShouldNotBeNull();
    }

    [Fact]
    public void Documents_ExposesAllDocumentModules()
    {
        typeof(Templating.GranitTemplatingModule).Assembly.ShouldNotBeNull();
        typeof(Templating.Scriban.GranitTemplatingScribanModule).Assembly.ShouldNotBeNull();
        typeof(DocumentGeneration.GranitDocumentGenerationModule).Assembly.ShouldNotBeNull();
        typeof(DocumentGeneration.Pdf.GranitDocumentGenerationPdfModule).Assembly.ShouldNotBeNull();
        typeof(DocumentGeneration.Excel.GranitDocumentGenerationExcelModule).Assembly.ShouldNotBeNull();
    }
}
