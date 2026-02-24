// =============================================================================
// Tests - BusinessRuleViolationException
// =============================================================================
// Verifies:
//   - Constructor initializes ErrorCode and Message correctly
//   - Default message falls back to the error code
//   - Inner exception is propagated
//   - Inherits from BusinessException (Liskov substitutability)
//   - Implements IHasErrorCode and IUserFriendlyException
// =============================================================================

using FluentAssertions;
using Granit.Core.Exceptions;
using Xunit;

namespace Granit.Core.Tests.Exceptions;

public sealed class BusinessRuleViolationExceptionTests
{
    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_SetsErrorCode()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.ErrorCode.Should().Be("Appointment:SlotUnavailable");
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable", "The requested time slot is no longer available.");

        exception.Message.Should().Be("The requested time slot is no longer available.");
    }

    [Fact]
    public void Constructor_WithoutMessage_UsesErrorCodeAsMessage()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.Message.Should().Be("Appointment:SlotUnavailable");
    }

    [Fact]
    public void Constructor_WithInnerException_PropagatesIt()
    {
        InvalidOperationException inner = new("inner");
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable", "message", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    // -------------------------------------------------------------------------
    // Inheritance — Liskov substitutability
    // -------------------------------------------------------------------------

    [Fact]
    public void IsAssignableFrom_BusinessException()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.Should().BeAssignableTo<BusinessException>();
    }

    [Fact]
    public void IsAssignableFrom_Exception()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.Should().BeAssignableTo<Exception>();
    }

    // -------------------------------------------------------------------------
    // Interface implementation
    // -------------------------------------------------------------------------

    [Fact]
    public void Implements_IHasErrorCode()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.Should().BeAssignableTo<IHasErrorCode>();
        ((IHasErrorCode)exception).ErrorCode.Should().Be("Appointment:SlotUnavailable");
    }

    [Fact]
    public void Implements_IUserFriendlyException()
    {
        BusinessRuleViolationException exception = new("Appointment:SlotUnavailable");

        exception.Should().BeAssignableTo<IUserFriendlyException>();
    }
}
