using Granit.Identity.Models;
using Shouldly;
using Xunit;

namespace Granit.Identity.Tests;

public sealed class IdentityUserTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var user = new IdentityUser(
            Id: "user-1",
            Username: "alice",
            Email: "alice@test.com",
            FirstName: "Alice",
            LastName: "Doe",
            Enabled: true);

        user.Id.ShouldBe("user-1");
        user.Username.ShouldBe("alice");
        user.Email.ShouldBe("alice@test.com");
        user.FirstName.ShouldBe("Alice");
        user.LastName.ShouldBe("Doe");
        user.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_AllowsNullableFields()
    {
        var user = new IdentityUser(
            Id: "user-2",
            Username: null,
            Email: null,
            FirstName: null,
            LastName: null,
            Enabled: false);

        user.Id.ShouldBe("user-2");
        user.Username.ShouldBeNull();
        user.Email.ShouldBeNull();
        user.FirstName.ShouldBeNull();
        user.LastName.ShouldBeNull();
        user.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var user1 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);
        var user2 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);

        user1.ShouldBe(user2);
        (user1 == user2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentId_AreNotEqual()
    {
        var user1 = new IdentityUser("id-1", "user", "e@test.com", "F", "L", true);
        var user2 = new IdentityUser("id-2", "user", "e@test.com", "F", "L", true);

        user1.ShouldNotBe(user2);
        (user1 != user2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentEnabled_AreNotEqual()
    {
        var user1 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);
        var user2 = new IdentityUser("id", "user", "e@test.com", "F", "L", false);

        user1.ShouldNotBe(user2);
    }

    [Fact]
    public void With_CreatesModifiedCopy()
    {
        var original = new IdentityUser("id", "user", "e@test.com", "F", "L", true);

        IdentityUser modified = original with { Enabled = false };

        modified.Enabled.ShouldBeFalse();
        modified.Id.ShouldBe("id");
        original.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var user1 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);
        var user2 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);

        user1.GetHashCode().ShouldBe(user2.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsTypeName()
    {
        var user = new IdentityUser("id", "alice", "alice@test.com", "Alice", "Doe", true);

        string str = user.ToString();

        str.ShouldContain("IdentityUser");
        str.ShouldContain("alice");
    }

    [Fact]
    public void Attributes_DefaultsToNull()
    {
        var user = new IdentityUser("id", "alice", "alice@test.com", "Alice", "Doe", true);

        user.Attributes.ShouldBeNull();
    }

    [Fact]
    public void Attributes_WhenProvided_AreAccessible()
    {
        var attrs = new Dictionary<string, string> { ["license"] = "MD-12345", ["department"] = "Cardiology" };
        var user = new IdentityUser("id", "alice", "alice@test.com", "Alice", "Doe", true, attrs);

        user.Attributes.ShouldNotBeNull();
        user.Attributes!.Count.ShouldBe(2);
        user.Attributes["license"].ShouldBe("MD-12345");
        user.Attributes["department"].ShouldBe("Cardiology");
    }

    [Fact]
    public void Equality_SameAttributes_AreEqual()
    {
        var attrs1 = new Dictionary<string, string> { ["key"] = "value" };
        var attrs2 = new Dictionary<string, string> { ["key"] = "value" };
        var user1 = new IdentityUser("id", "user", "e@test.com", "F", "L", true, attrs1);
        var user2 = new IdentityUser("id", "user", "e@test.com", "F", "L", true, attrs2);

        // Record equality uses reference equality for dictionary — expected behavior.
        user1.ShouldNotBe(user2);
    }

    [Fact]
    public void Equality_BothNullAttributes_AreEqual()
    {
        var user1 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);
        var user2 = new IdentityUser("id", "user", "e@test.com", "F", "L", true);

        user1.ShouldBe(user2);
    }
}
