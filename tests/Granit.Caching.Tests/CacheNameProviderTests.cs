// =============================================================================
// Tests - CacheNameProvider
// =============================================================================
// Vérifie la convention de nommage des entrées de cache :
//   - "UserCacheItem" → "User" (suffixe CacheItem supprimé)
//   - [CacheName("custom")] surcharge la convention
//   - Type sans suffixe CacheItem → nom intact
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class CacheNameProviderTests
{
    [Fact]
    public void GetCacheName_TypeWithCacheItemSuffix_RemovesSuffix()
    {
        // Act
        string name = CacheNameProvider.GetCacheName(typeof(UserCacheItem));

        // Assert
        name.ShouldBe("User");
    }

    [Fact]
    public void GetCacheName_TypeWithCacheNameAttribute_UsesAttributeValue()
    {
        // Act
        string name = CacheNameProvider.GetCacheName(typeof(CustomNamedItem));

        // Assert
        name.ShouldBe("MyCustomCache");
    }

    [Fact]
    public void GetCacheName_TypeWithoutSuffix_UsesTypeName()
    {
        // Act
        string name = CacheNameProvider.GetCacheName(typeof(ProductDto));

        // Assert
        name.ShouldBe("ProductDto");
    }

    [Fact]
    public void GetCacheName_CalledTwiceForSameType_ReturnsSameValue()
    {
        // Act
        string name1 = CacheNameProvider.GetCacheName(typeof(UserCacheItem));
        string name2 = CacheNameProvider.GetCacheName(typeof(UserCacheItem));

        // Assert — résultat mis en cache interne, doit être identique
        name1.ShouldBe(name2);
    }

    // Types de test internes
    private sealed class UserCacheItem { }

    [CacheName("MyCustomCache")]
    private sealed class CustomNamedItem { }

    private sealed class ProductDto { }
}
