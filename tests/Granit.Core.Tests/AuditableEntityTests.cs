// =============================================================================
// Tests - AuditableEntity
// =============================================================================
// Vérifie les valeurs par défaut et l'assignation des propriétés de l'entité
// auditable de base (trail HDS : créé/modifié).
// =============================================================================

using FluentAssertions;
using Granit.Core.Domain;
using Xunit;

namespace Granit.Core.Tests;

public sealed class AuditableEntityTests
{
    [Fact]
    public void AuditableEntity_DefaultValues_AreCorrect()
    {
        // Act
        ConcreteAuditableEntity entity = new();

        // Assert
        entity.Id.Should().Be(Guid.Empty);
        entity.CreatedAt.Should().Be(default);
        entity.CreatedBy.Should().BeEmpty("default is string.Empty");
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void AuditableEntity_Properties_CanBeAssigned()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset modified = now.AddHours(1);

        // Act
        ConcreteAuditableEntity entity = new()
        {
            Id = id,
            CreatedAt = now,
            CreatedBy = "user-123",
            ModifiedAt = modified,
            ModifiedBy = "user-456",
        };

        // Assert
        entity.Id.Should().Be(id);
        entity.CreatedAt.Should().Be(now);
        entity.CreatedBy.Should().Be("user-123");
        entity.ModifiedAt.Should().Be(modified);
        entity.ModifiedBy.Should().Be("user-456");
    }

    /// <summary>Concrete subclass required to instantiate the abstract base.</summary>
    private sealed class ConcreteAuditableEntity : AuditableEntity;
}
