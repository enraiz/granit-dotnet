// =============================================================================
// Tests - BusinessException
// =============================================================================
// Verifies:
//   - Constructor initializes ErrorCode and Message correctly
//   - Default message falls back to the error code
//   - Inner exception is propagated
//   - Implements IHasErrorCode and IUserFriendlyException
// =============================================================================

using DigitalDynamics.Foundation.Core.Exceptions;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests.Exceptions;

public sealed class BusinessExceptionTests
{
    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_SetsErrorCode()
    {
        BusinessException exception = new("Vault:CredentialsFailed");

        exception.ErrorCode.Should().Be("Vault:CredentialsFailed");
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        BusinessException exception = new("Vault:CredentialsFailed", "Connexion Vault échouée.");

        exception.Message.Should().Be("Connexion Vault échouée.");
    }

    [Fact]
    public void Constructor_WithoutMessage_UsesErrorCodeAsMessage()
    {
        BusinessException exception = new("Vault:CredentialsFailed");

        exception.Message.Should().Be("Vault:CredentialsFailed");
    }

    [Fact]
    public void Constructor_WithInnerException_PropagatesIt()
    {
        InvalidOperationException inner = new("inner");
        BusinessException exception = new("Vault:CredentialsFailed", "message", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    // -------------------------------------------------------------------------
    // Interface implementation
    // -------------------------------------------------------------------------

    [Fact]
    public void Implements_IHasErrorCode()
    {
        BusinessException exception = new("Vault:CredentialsFailed");

        exception.Should().BeAssignableTo<IHasErrorCode>();
        ((IHasErrorCode)exception).ErrorCode.Should().Be("Vault:CredentialsFailed");
    }

    [Fact]
    public void Implements_IUserFriendlyException()
    {
        BusinessException exception = new("Vault:CredentialsFailed");

        exception.Should().BeAssignableTo<IUserFriendlyException>();
    }

    [Fact]
    public void IsException()
    {
        BusinessException exception = new("Vault:CredentialsFailed");

        exception.Should().BeAssignableTo<Exception>();
    }
}
