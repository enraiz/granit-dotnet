using Granit.Identity.Events;
using Granit.Identity.Extensions;
using Granit.Identity.Internal;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Identity.Tests;

public sealed class IdentityEventPublisherTests
{
    private readonly NullIdentityEventPublisher _publisher = new();

    [Fact]
    public void ImplementsIIdentityEventPublisher() =>
        _publisher.ShouldBeAssignableTo<IIdentityEventPublisher>();

    [Fact]
    public async Task PublishAsync_CompletesWithoutError()
    {
        var evt = new IdentityUserCreatedEvent("u1", "alice", "alice@test.com");

        await Should.NotThrowAsync(
            () => _publisher.PublishAsync(evt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublishAsync_WithEnabledChangedEvent_CompletesWithoutError()
    {
        var evt = new IdentityUserEnabledChangedEvent("u1", true);

        await Should.NotThrowAsync(
            () => _publisher.PublishAsync(evt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublishAsync_WithRoleAssignedEvent_CompletesWithoutError()
    {
        var evt = new IdentityRoleAssignedEvent("u1", "admin");

        await Should.NotThrowAsync(
            () => _publisher.PublishAsync(evt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AddGranitIdentity_RegistersNullIdentityEventPublisherByDefault()
    {
        ServiceCollection services = new();

        services.AddGranitIdentity();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityEventPublisher eventPublisher = scope.ServiceProvider
            .GetRequiredService<IIdentityEventPublisher>();

        eventPublisher.ShouldBeOfType<NullIdentityEventPublisher>();
    }

    [Fact]
    public void AddGranitIdentity_RegistersEventPublisherAsScoped()
    {
        ServiceCollection services = new();

        services.AddGranitIdentity();

        ServiceDescriptor descriptor = services.Single(
            d => d.ServiceType == typeof(IIdentityEventPublisher));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitIdentity_DoesNotOverrideExistingEventPublisher()
    {
        ServiceCollection services = new();
        services.AddScoped<IIdentityEventPublisher, NullIdentityEventPublisher>();

        services.AddGranitIdentity();

        // TryAddScoped should not replace the already-registered publisher.
        services.Count(d => d.ServiceType == typeof(IIdentityEventPublisher)).ShouldBe(1);
    }
}
