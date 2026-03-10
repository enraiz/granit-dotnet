using Granit.Authentication.ApiKeys.Events;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Tests;

public sealed class ApiKeyCreatedEventTests
{
    [Fact]
    public void Properties_AreSetFromConstructor()
    {
        var id = Guid.NewGuid();
        var evt = new ApiKeyCreatedEvent(id, "Partner Key", ApiKeyType.Secret);

        evt.ApiKeyId.ShouldBe(id);
        evt.Name.ShouldBe("Partner Key");
        evt.Type.ShouldBe(ApiKeyType.Secret);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var evt1 = new ApiKeyCreatedEvent(id, "Key A", ApiKeyType.Publishable);
        var evt2 = new ApiKeyCreatedEvent(id, "Key A", ApiKeyType.Publishable);

        evt1.ShouldBe(evt2);
    }

    [Fact]
    public void RecordInequality_DifferentType()
    {
        var id = Guid.NewGuid();
        var evt1 = new ApiKeyCreatedEvent(id, "Key A", ApiKeyType.Secret);
        var evt2 = new ApiKeyCreatedEvent(id, "Key A", ApiKeyType.Publishable);

        evt1.ShouldNotBe(evt2);
    }
}

public sealed class ApiKeyRevokedEventTests
{
    [Fact]
    public void Properties_AreSetFromConstructor()
    {
        var id = Guid.NewGuid();
        var evt = new ApiKeyRevokedEvent(id, "abc123hash");

        evt.ApiKeyId.ShouldBe(id);
        evt.HashedKey.ShouldBe("abc123hash");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var evt1 = new ApiKeyRevokedEvent(id, "hash1");
        var evt2 = new ApiKeyRevokedEvent(id, "hash1");

        evt1.ShouldBe(evt2);
    }
}

public sealed class ApiKeyRotatedEventTests
{
    [Fact]
    public void Properties_AreSetFromConstructor()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var evt = new ApiKeyRotatedEvent(oldId, newId, "oldhash");

        evt.OldApiKeyId.ShouldBe(oldId);
        evt.NewApiKeyId.ShouldBe(newId);
        evt.OldHashedKey.ShouldBe("oldhash");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var evt1 = new ApiKeyRotatedEvent(oldId, newId, "hash");
        var evt2 = new ApiKeyRotatedEvent(oldId, newId, "hash");

        evt1.ShouldBe(evt2);
    }
}

public sealed class ApiKeyScopesUpdatedEventTests
{
    [Fact]
    public void Properties_AreSetFromConstructor()
    {
        var id = Guid.NewGuid();
        var evt = new ApiKeyScopesUpdatedEvent(id, "scopehash");

        evt.ApiKeyId.ShouldBe(id);
        evt.HashedKey.ShouldBe("scopehash");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var evt1 = new ApiKeyScopesUpdatedEvent(id, "hash");
        var evt2 = new ApiKeyScopesUpdatedEvent(id, "hash");

        evt1.ShouldBe(evt2);
    }
}
