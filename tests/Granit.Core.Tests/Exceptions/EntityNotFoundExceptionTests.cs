// =============================================================================
// Tests - EntityNotFoundException
// =============================================================================
// Verifies:
//   - Constructor stores EntityType and EntityId
//   - Message includes entity type name and id
//   - Implements IUserFriendlyException
//   - Does NOT implement IHasErrorCode (by design — entity names must not leak as error keys)
// =============================================================================

using Granit.Core.Exceptions;
using Shouldly;
using Xunit;

namespace Granit.Core.Tests.Exceptions;

public sealed class EntityNotFoundExceptionTests
{
    private sealed class Appointment;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_SetsEntityType()
    {
        EntityNotFoundException exception = new(typeof(Appointment), Guid.NewGuid());

        exception.EntityType.ShouldBe(typeof(Appointment));
    }

    [Fact]
    public void Constructor_SetsEntityId()
    {
        var id = Guid.NewGuid();
        EntityNotFoundException exception = new(typeof(Appointment), id);

        exception.EntityId.ShouldBe(id);
    }

    [Fact]
    public void Constructor_MessageContainsEntityTypeName()
    {
        EntityNotFoundException exception = new(typeof(Appointment), 42);

        exception.Message.ShouldContain("Appointment");
    }

    [Fact]
    public void Constructor_MessageContainsEntityId()
    {
        var id = Guid.NewGuid();
        EntityNotFoundException exception = new(typeof(Appointment), id);

        exception.Message.ShouldContain(id.ToString());
    }

    // -------------------------------------------------------------------------
    // Interface implementation
    // -------------------------------------------------------------------------

    [Fact]
    public void Implements_IUserFriendlyException()
    {
        EntityNotFoundException exception = new(typeof(Appointment), 1);

        exception.ShouldBeAssignableTo<IUserFriendlyException>();
    }

    [Fact]
    public void DoesNotImplement_IHasErrorCode()
    {
        EntityNotFoundException exception = new(typeof(Appointment), 1);

        // By design: entity type names must not be used as localizable error code keys.
        exception.ShouldNotBeAssignableTo<IHasErrorCode>();
    }

    [Fact]
    public void IsException()
    {
        EntityNotFoundException exception = new(typeof(Appointment), 1);

        exception.ShouldBeAssignableTo<Exception>();
    }
}
