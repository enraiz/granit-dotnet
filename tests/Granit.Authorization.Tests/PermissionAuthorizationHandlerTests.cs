// =============================================================================
// Tests - PermissionAuthorizationHandler
// =============================================================================
// Vérifie que le handler appelle Succeed lorsque la permission est accordée,
// et ne le fait pas lorsqu'elle est refusée.
// =============================================================================

using System.Security.Claims;
using FluentAssertions;
using Granit.Authorization.Abstractions;
using Granit.Authorization.Authorization;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_PermissionGranted_ContextSucceeds()
    {
        // Arrange
        IPermissionChecker checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync("Invoices.Delete", Arg.Any<CancellationToken>()).Returns(true);

        PermissionAuthorizationHandler handler = new(checker);
        PermissionRequirement requirement = new("Invoices.Delete");
        AuthorizationHandlerContext context = new(
            [requirement],
            new ClaimsPrincipal(),
            resource: null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_PermissionDenied_ContextDoesNotSucceed()
    {
        // Arrange
        IPermissionChecker checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync("Invoices.Delete", Arg.Any<CancellationToken>()).Returns(false);

        PermissionAuthorizationHandler handler = new(checker);
        PermissionRequirement requirement = new("Invoices.Delete");
        AuthorizationHandlerContext context = new(
            [requirement],
            new ClaimsPrincipal(),
            resource: null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }
}
