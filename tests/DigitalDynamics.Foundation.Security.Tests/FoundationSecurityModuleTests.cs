// =============================================================================
// Tests - FoundationSecurityModule
// =============================================================================
// FoundationSecurityModule is an abstractions marker module.
// No services registered — the JWT implementation is in
// Foundation.Authentication.JwtBearer (FoundationJwtBearerModule).
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class FoundationSecurityModuleTests
{
    [Fact]
    public void FoundationSecurityModule_IsFoundationModule() => typeof(FoundationSecurityModule).Should().BeAssignableTo<FoundationModule>();
}
