// =============================================================================
// Tests - IDomainEvent / IIntegrationEvent contracts
// =============================================================================
// Verifies that both marker interfaces declare no members, ensuring they remain
// pure markers and do not impose any implementation burden on consumers.
// =============================================================================

using System.Reflection;
using FluentAssertions;
using Granit.Core.Events;
using Xunit;

namespace Granit.Core.Tests.Events;

public sealed class DomainEventContractTests
{
    [Fact]
    public void IDomainEvent_HasNoMembers() =>
        typeof(IDomainEvent).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().BeEmpty("IDomainEvent must be a pure marker interface");

    [Fact]
    public void IIntegrationEvent_HasNoMembers() =>
        typeof(IIntegrationEvent).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().BeEmpty("IIntegrationEvent must be a pure marker interface");

    [Fact]
    public void IDomainEvent_CanBeImplementedByRecord()
    {
        ConcreteDomainEvent evt = new(Guid.NewGuid());

        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void IIntegrationEvent_CanBeImplementedByRecord()
    {
        ConcreteIntegrationEvent evt = new(Guid.NewGuid(), "bed-42");

        evt.Should().BeAssignableTo<IIntegrationEvent>();
    }

    [Fact]
    public void IDomainEvent_AndIIntegrationEvent_AreInCorrectNamespace()
    {
        typeof(IDomainEvent).Namespace.Should().Be("Granit.Core.Events");
        typeof(IIntegrationEvent).Namespace.Should().Be("Granit.Core.Events");
    }

    // --- Test fixtures ---

    private sealed record ConcreteDomainEvent(Guid PatientId) : IDomainEvent;

    private sealed record ConcreteIntegrationEvent(Guid PatientId, string BedCode) : IIntegrationEvent;
}
