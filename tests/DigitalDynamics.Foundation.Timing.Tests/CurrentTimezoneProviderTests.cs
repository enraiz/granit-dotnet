// =============================================================================
// Tests - CurrentTimezoneProvider
// =============================================================================
// Verifie que CurrentTimezoneProvider :
//   - Stocke et retourne la timezone correctement
//   - Isole les valeurs entre contextes async (AsyncLocal)
//   - Retourne null par defaut
// =============================================================================

using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Timing.Tests;

public sealed class CurrentTimezoneProviderTests
{
    [Fact]
    public void Timezone_DefaultsToNull()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider();

        // Assert
        provider.Timezone.Should().BeNull();
    }

    [Fact]
    public void Timezone_CanBeSetAndRead()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider();

        // Act
        provider.Timezone = "Europe/Brussels";

        // Assert
        provider.Timezone.Should().Be("Europe/Brussels");
    }

    [Fact]
    public void Timezone_CanBeResetToNull()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider();
        provider.Timezone = "Europe/Brussels";

        // Act
        provider.Timezone = null;

        // Assert
        provider.Timezone.Should().BeNull();
    }

    [Fact]
    public async Task Timezone_IsIsolatedPerAsyncContext()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider();
        provider.Timezone = "Europe/Brussels";

        string? innerTimezone = null;

        // Act - lancer un nouveau contexte async
        await Task.Run(() =>
        {
            // Le nouveau contexte async herite de la valeur parente
            innerTimezone = provider.Timezone;
            // Modifier dans le contexte enfant
            provider.Timezone = "America/New_York";
        }, TestContext.Current.CancellationToken);

        // Assert - la valeur dans le contexte parent n'est pas affectee
        provider.Timezone.Should().Be("Europe/Brussels",
            "AsyncLocal isole les modifications du contexte enfant");
    }
}
