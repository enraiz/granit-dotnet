// =============================================================================
// Tests - HybridCachingOptions
// =============================================================================
// Vérifie que LocalCacheExpiration respecte les contraintes Kubernetes :
//   - Valeur par défaut ≤ 60 s (borne la staleness inter-pods)
//   - La section de configuration est correcte
// =============================================================================

using FluentAssertions;
using Xunit;

namespace Granit.Caching.Hybrid.Tests;

public sealed class HybridCachingOptionsTests
{
    [Fact]
    public void DefaultLocalCacheExpiration_IsAtMostSixtySeconds()
    {
        // Arrange & Act
        HybridCachingOptions options = new();

        // Assert — valeur par défaut bornée pour limiter la staleness inter-pods Kubernetes
        options.LocalCacheExpiration.TotalSeconds.Should().BeLessThanOrEqualTo(
            60,
            "LocalCacheExpiration doit être ≤ 60 s pour borner la fenêtre de données obsolètes entre pods");
    }

    [Fact]
    public void DefaultLocalCacheExpiration_IsThirtySeconds()
    {
        // Act
        HybridCachingOptions options = new();

        // Assert
        options.LocalCacheExpiration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        HybridCachingOptions.SectionName.Should().Be("Cache:Hybrid");
}
