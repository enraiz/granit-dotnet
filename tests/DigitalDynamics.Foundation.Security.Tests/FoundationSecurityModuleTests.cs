// =============================================================================
// Tests - FoundationSecurityModule
// =============================================================================
// FoundationSecurityModule est un module marqueur d'abstractions.
// Aucun service enregistré — l'implémentation JWT est dans
// Foundation.Authentication.JwtBearer (FoundationJwtBearerModule).
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class FoundationSecurityModuleTests
{
    [Fact]
    public void FoundationSecurityModule_IsFoundationModule()
    {
        typeof(FoundationSecurityModule).Should().BeAssignableTo<FoundationModule>();
    }
}
