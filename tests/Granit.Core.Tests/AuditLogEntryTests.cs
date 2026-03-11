// =============================================================================
// Tests - AuditLogEntry
// =============================================================================
// Vérifie que les propriétés de l'entrée d'audit ISO 27001 fonctionnent correctement.
// =============================================================================

using Granit.Core.Domain;
using Shouldly;
using Xunit;

namespace Granit.Core.Tests;

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
        entry.Id.ShouldBe(id);
        entry.Timestamp.ShouldBe(timestamp);
        entry.UserId.ShouldBe("user-123");
        entry.Operation.ShouldBe("Create");
        entry.EntityType.ShouldBe("Patient");
        entry.EntityId.ShouldBe("entity-456");
        entry.Changes.ShouldBe("""{"Name": "Updated"}""");
        entry.IpAddress.ShouldBe("192.168.1.1");
        entry.UserAgent.ShouldBe("Mozilla/5.0");
    }

    [Fact]
    public void AuditLogEntry_DefaultValues_AreCorrect()
    {
        // Act
        AuditLogEntry entry = new();

        // Assert
        entry.Id.ShouldBe(Guid.Empty);
        entry.Timestamp.ShouldBe(default);
        entry.UserId.ShouldBeEmpty();
        entry.Operation.ShouldBeEmpty();
        entry.EntityType.ShouldBeEmpty();
        entry.EntityId.ShouldBeEmpty();
        entry.Changes.ShouldBeNull();
        entry.IpAddress.ShouldBeNull();
        entry.UserAgent.ShouldBeNull();
    }
}
