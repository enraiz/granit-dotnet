// =============================================================================
// Tests - AuditLogEntry
// =============================================================================
// Vérifie que les propriétés de l'entrée d'audit HDS fonctionnent correctement.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests;

public sealed class AuditLogEntryTests
{
    [Fact]
    public void AuditLogEntry_PropertiesAreSetCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        AuditLogEntry entry = new()
        {
            Id = id,
            Timestamp = timestamp,
            UserId = "user-123",
            Operation = "Create",
            EntityType = "Patient",
            EntityId = "entity-456",
            Changes = """{"Name": "Updated"}""",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Assert
        entry.Id.Should().Be(id);
        entry.Timestamp.Should().Be(timestamp);
        entry.UserId.Should().Be("user-123");
        entry.Operation.Should().Be("Create");
        entry.EntityType.Should().Be("Patient");
        entry.EntityId.Should().Be("entity-456");
        entry.Changes.Should().Be("""{"Name": "Updated"}""");
        entry.IpAddress.Should().Be("192.168.1.1");
        entry.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void AuditLogEntry_DefaultValues_AreCorrect()
    {
        // Act
        AuditLogEntry entry = new();

        // Assert
        entry.Id.Should().Be(Guid.Empty);
        entry.Timestamp.Should().Be(default);
        entry.UserId.Should().BeEmpty();
        entry.Operation.Should().BeEmpty();
        entry.EntityType.Should().BeEmpty();
        entry.EntityId.Should().BeEmpty();
        entry.Changes.Should().BeNull();
        entry.IpAddress.Should().BeNull();
        entry.UserAgent.Should().BeNull();
    }
}
