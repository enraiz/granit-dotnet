// =============================================================================
// Tests - WolverineValidationServiceCollectionExtensions
// =============================================================================
// Verifies that AddGranitValidatorsFromWolverineHandlerModules() discovers and
// registers IValidator<T> implementations from assemblies decorated with
// [assembly: WolverineHandlerModule].
// =============================================================================

using FluentValidation;
using Granit.Wolverine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class WolverineValidationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitValidatorsFromWolverineHandlerModules_RegistersValidatorFromMarkedAssembly()
    {
        // This test assembly is decorated with [assembly: WolverineHandlerModule]
        // (see AssemblyInfo.cs) and contains DummyCommandValidator below.
        ServiceCollection services = new();

        services.AddGranitValidatorsFromWolverineHandlerModules();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidator<DummyCommand>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitValidatorsFromWolverineHandlerModules_RegistersInternalValidators()
    {
        ServiceCollection services = new();

        services.AddGranitValidatorsFromWolverineHandlerModules();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidator<InternalDummyCommand>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitValidatorsFromWolverineHandlerModules_DoesNotThrow_WhenNoMarkedAssemblies()
    {
        // Even if some loaded assemblies have no attribute, the method should not throw.
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitValidatorsFromWolverineHandlerModules());
    }
}

// --- Test fixtures (public + internal) ---

public sealed record DummyCommand(string Name);

public sealed class DummyCommandValidator : AbstractValidator<DummyCommand>
{
    public DummyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

internal sealed record InternalDummyCommand(string Value);

internal sealed class InternalDummyCommandValidator : AbstractValidator<InternalDummyCommand>
{
    public InternalDummyCommandValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
    }
}
