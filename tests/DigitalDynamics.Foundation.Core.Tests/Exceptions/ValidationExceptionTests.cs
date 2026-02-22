// =============================================================================
// Tests - ValidationException
// =============================================================================
// Verifies:
//   - Constructor stores ValidationErrors correctly
//   - Message is always the standard generic validation message
//   - Implements IHasValidationErrors and IUserFriendlyException
// =============================================================================

using DigitalDynamics.Foundation.Core.Exceptions;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests.Exceptions;

public sealed class ValidationExceptionTests
{
    private static Dictionary<string, string[]> BuildErrors() =>
        new Dictionary<string, string[]>
        {
            ["Email"] = ["The Email field is required."],
            ["Name"] = ["Must be between 3 and 50 characters."]
        };

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_StoresValidationErrors()
    {
        IReadOnlyDictionary<string, string[]> errors = BuildErrors();
        ValidationException exception = new(errors);

        exception.ValidationErrors.Should().BeSameAs(errors);
    }

    [Fact]
    public void Constructor_EmailErrors_AreAccessible()
    {
        ValidationException exception = new(BuildErrors());

        exception.ValidationErrors["Email"].Should().ContainSingle()
            .Which.Should().Be("The Email field is required.");
    }

    [Fact]
    public void Constructor_MultipleFieldErrors_AreAllStored()
    {
        ValidationException exception = new(BuildErrors());

        exception.ValidationErrors.Should().HaveCount(2);
        exception.ValidationErrors.Keys.Should().Contain(["Email", "Name"]);
    }

    [Fact]
    public void Constructor_SetsGenericMessage()
    {
        ValidationException exception = new(BuildErrors());

        exception.Message.Should().Be("One or more validation errors occurred.");
    }

    // -------------------------------------------------------------------------
    // Interface implementation
    // -------------------------------------------------------------------------

    [Fact]
    public void Implements_IHasValidationErrors()
    {
        ValidationException exception = new(BuildErrors());

        exception.Should().BeAssignableTo<IHasValidationErrors>();
    }

    [Fact]
    public void Implements_IUserFriendlyException()
    {
        ValidationException exception = new(BuildErrors());

        exception.Should().BeAssignableTo<IUserFriendlyException>();
    }

    [Fact]
    public void IsException()
    {
        ValidationException exception = new(BuildErrors());

        exception.Should().BeAssignableTo<Exception>();
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_EmptyErrors_IsAllowed()
    {
        IReadOnlyDictionary<string, string[]> empty = new Dictionary<string, string[]>();
        ValidationException exception = new(empty);

        exception.ValidationErrors.Should().BeEmpty();
    }
}
