using Granit.Privacy.DataDeletion.Events;
using Shouldly;
using Xunit;

namespace Granit.Privacy.Tests.DataDeletion.Events;

public sealed class PersonalDataDeletionRequestedEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        DateTimeOffset requestedAt = DateTimeOffset.UtcNow;

        var sut = new PersonalDataDeletionRequestedEvent(
            requestId,
            userId,
            "dpo@example.com",
            requestedAt,
            "GDPR Art. 17 request");

        sut.RequestId.ShouldBe(requestId);
        sut.UserId.ShouldBe(userId);
        sut.RequestedBy.ShouldBe("dpo@example.com");
        sut.RequestedAt.ShouldBe(requestedAt);
        sut.Reason.ShouldBe("GDPR Art. 17 request");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        DateTimeOffset requestedAt = DateTimeOffset.UtcNow;

        var a = new PersonalDataDeletionRequestedEvent(requestId, userId, "admin", requestedAt, "reason");
        var b = new PersonalDataDeletionRequestedEvent(requestId, userId, "admin", requestedAt, "reason");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        DateTimeOffset requestedAt = DateTimeOffset.UtcNow;

        PersonalDataDeletionRequestedEvent a = new(Guid.NewGuid(), Guid.NewGuid(), "a", requestedAt, "r1");
        PersonalDataDeletionRequestedEvent b = new(Guid.NewGuid(), Guid.NewGuid(), "b", requestedAt, "r2");

        a.ShouldNotBe(b);
    }
}
