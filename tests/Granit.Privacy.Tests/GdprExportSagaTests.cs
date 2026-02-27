using FluentAssertions;
using Granit.Privacy.DataExport;
using Granit.Privacy.DataExport.Events;
using Granit.Privacy.DataExport.Internal;
using Granit.Privacy.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wolverine;
using Xunit;
using Xunit.v3;

namespace Granit.Privacy.Tests;

public sealed class GdprExportSagaTests
{
    private static IOptions<GranitPrivacyOptions> DefaultOptions() =>
        Microsoft.Extensions.Options.Options.Create(new GranitPrivacyOptions { ExportTimeoutMinutes = 5 });

    private static DataProviderRegistry BuildRegistry(params string[] providers)
    {
        DataProviderRegistry registry = new();
        foreach (string p in providers)
        {
            registry.Register(p);
        }

        return registry;
    }

    // -------------------------------------------------------------------------
    // Scénario 1 : export complet avec plusieurs modules
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartAsync_InitializesState_FromEvent()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("patients", "billing");
        Guid requestId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        PersonalDataRequestedEvent evt = new(requestId, userId, DateTimeOffset.UtcNow);

        await saga.StartAsync(evt, registry, DefaultOptions(), context);

        saga.Id.Should().Be(requestId);
        saga.UserId.Should().Be(userId);
        saga.ExpectedCount.Should().Be(2);
        saga.PendingProviders.Should().Contain("patients").And.Contain("billing");
    }

    [Fact]
    public async Task StartAsync_SchedulesTimeout_ViaPublishAsync()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("patients", "billing");
        Guid requestId = Guid.NewGuid();
        PersonalDataRequestedEvent evt = new(requestId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await saga.StartAsync(evt, registry, DefaultOptions(), context);

        // ScheduleAsync is an extension method that calls PublishAsync with DeliveryOptions.
        // NSubstitute cannot intercept extension methods, so we verify the underlying PublishAsync call.
        await context.Received(1).PublishAsync(
            Arg.Is<ExportTimedOutEvent>(t => t.RequestId == requestId),
            Arg.Any<DeliveryOptions>());
    }

    [Fact]
    public async Task Handle_PreparedEvent_ReturnsNullUntilAllFragmentsArrived()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("patients", "billing", "appointments");
        PersonalDataRequestedEvent startEvt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        await saga.StartAsync(startEvt, registry, DefaultOptions(), context);

        ExportCompletedEvent? result1 = saga.Handle(
            new PersonalDataPreparedEvent(startEvt.RequestId, "patients", "blob-1", "application/json"));
        ExportCompletedEvent? result2 = saga.Handle(
            new PersonalDataPreparedEvent(startEvt.RequestId, "billing", "blob-2", "application/json"));

        result1.Should().BeNull();
        result2.Should().BeNull();
        saga.ReceivedFragments.Should().HaveCount(2);
        saga.PendingProviders.Should().ContainSingle("appointments");
    }

    [Fact]
    public async Task Handle_PreparedEvent_ReturnsCompletedEvent_WhenAllFragmentsArrived()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("patients", "billing");
        PersonalDataRequestedEvent startEvt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        await saga.StartAsync(startEvt, registry, DefaultOptions(), context);

        saga.Handle(new PersonalDataPreparedEvent(startEvt.RequestId, "patients", "blob-patients", "application/json"));
        ExportCompletedEvent? result = saga.Handle(
            new PersonalDataPreparedEvent(startEvt.RequestId, "billing", "blob-billing", "application/json"));

        result.Should().NotBeNull();
        result!.RequestId.Should().Be(startEvt.RequestId);
        result.UserId.Should().Be(startEvt.UserId);
        result.IsPartial.Should().BeFalse();
        result.MissingProviders.Should().BeEmpty();
        result.ArchiveBlobReferenceId.Should().Be($"gdpr-export/{startEvt.RequestId}");
    }

    // -------------------------------------------------------------------------
    // Scénario 2 : export partiel avec timeout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Timeout_ReturnsPartialEvent_WithMissingProviders()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("patients", "billing", "appointments");
        PersonalDataRequestedEvent startEvt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        await saga.StartAsync(startEvt, registry, DefaultOptions(), context);

        saga.Handle(new PersonalDataPreparedEvent(startEvt.RequestId, "patients", "blob-patients", "application/json"));
        saga.Handle(new PersonalDataPreparedEvent(startEvt.RequestId, "billing", "blob-billing", "application/json"));
        ExportCompletedEvent result = saga.Handle(new ExportTimedOutEvent(startEvt.RequestId));

        result.IsPartial.Should().BeTrue();
        result.MissingProviders.Should().ContainSingle("appointments");
        result.ArchiveBlobReferenceId.Should().Be($"gdpr-export/{startEvt.RequestId}");
    }

    // -------------------------------------------------------------------------
    // Scénario 3 : aucun provider enregistré
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartAsync_NoProviders_CompletesImmediately_WithEmptyEvent()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry emptyRegistry = new();
        PersonalDataRequestedEvent evt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        ExportCompletedEvent? result = await saga.StartAsync(evt, emptyRegistry, DefaultOptions(), context);

        result.Should().NotBeNull();
        result!.IsPartial.Should().BeFalse();
        result.MissingProviders.Should().BeEmpty();

        // No timeout scheduled when there are no providers
        await context.DidNotReceive().PublishAsync(
            Arg.Any<ExportTimedOutEvent>(),
            Arg.Any<DeliveryOptions>());
    }

    // -------------------------------------------------------------------------
    // Scénario 4 : conformité HDS — les events ne transportent que des BlobReferenceId
    // -------------------------------------------------------------------------

    [Fact]
    public void PersonalDataPreparedEvent_ContainsOnlyBlobReferenceId_NotRawData()
    {
        // Structural contract: the event record only carries a BlobReferenceId,
        // never raw personal data — enforced by the type definition (HDS compliance).
        PersonalDataPreparedEvent evt = new(
            Guid.NewGuid(), "patients", "blob-ref-123", "application/json");

        evt.BlobReferenceId.Should().Be("blob-ref-123");

        System.Reflection.PropertyInfo[] properties =
            typeof(PersonalDataPreparedEvent).GetProperties();
        string[] allowedProperties =
            ["RequestId", "ProviderName", "BlobReferenceId", "ContentType", "EqualityContract"];
        properties.Select(p => p.Name).Should().OnlyContain(name => allowedProperties.Contains(name));
    }

    // -------------------------------------------------------------------------
    // Timeout configuration
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartAsync_UsesConfiguredTimeout_InScheduledTime()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        IOptions<GranitPrivacyOptions> options = Microsoft.Extensions.Options.Options.Create(
            new GranitPrivacyOptions { ExportTimeoutMinutes = 10 });
        DataProviderRegistry registry = BuildRegistry("auth");
        PersonalDataRequestedEvent evt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        await saga.StartAsync(evt, registry, options, context);

        // ScheduleAsync(message, TimeSpan) sets ScheduleDelay (relative), not ScheduledTime (absolute).
        await context.Received(1).PublishAsync(
            Arg.Any<ExportTimedOutEvent>(),
            Arg.Is<DeliveryOptions>(o =>
                o.ScheduleDelay.HasValue &&
                o.ScheduleDelay.Value == TimeSpan.FromMinutes(10)));
    }

    // -------------------------------------------------------------------------
    // ArchiveBlobReferenceId convention
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExportCompletedEvent_ArchiveBlobReferenceId_UsesGdprExportConvention()
    {
        GdprExportSaga saga = new();
        IMessageContext context = Substitute.For<IMessageContext>();
        DataProviderRegistry registry = BuildRegistry("auth");
        PersonalDataRequestedEvent startEvt = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        await saga.StartAsync(startEvt, registry, DefaultOptions(), context);

        ExportCompletedEvent? result = saga.Handle(
            new PersonalDataPreparedEvent(startEvt.RequestId, "auth", "blob-auth", "application/json"));

        result!.ArchiveBlobReferenceId.Should().Be($"gdpr-export/{startEvt.RequestId}");
    }
}
