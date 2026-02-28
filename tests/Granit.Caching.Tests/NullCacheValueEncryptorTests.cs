// =============================================================================
// Tests - NullCacheValueEncryptor
// =============================================================================
// Vérifie que l'implémentation no-op retourne les données en entrée sans
// modification (identité pour Encrypt et Decrypt).
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class NullCacheValueEncryptorTests
{
    [Fact]
    public void Encrypt_ReturnsInputUnchanged()
    {
        NullCacheValueEncryptor encryptor = new();
        byte[] data = [1, 2, 3, 4, 5];

        byte[] result = encryptor.Encrypt(data);

        result.ShouldBeSameAs(data);
    }

    [Fact]
    public void Decrypt_ReturnsInputUnchanged()
    {
        NullCacheValueEncryptor encryptor = new();
        byte[] data = [10, 20, 30];

        byte[] result = encryptor.Decrypt(data);

        result.ShouldBeSameAs(data);
    }
}
