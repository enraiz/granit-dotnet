using Granit.Privacy.DataDeletion;
using Granit.Privacy.DataDeletion.Events;
using Shouldly;
using Xunit;

namespace Granit.Privacy.Tests.DataDeletion.Events;

public sealed class PersonalDataDeletedEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var requestId = Guid.NewGuid();

        var sut = new PersonalDataDeletedEvent(
            requestId,
            "patients",
            DeletionAction.PhysicalDelete,
            42,
            "All patient records deleted");

        sut.RequestId.ShouldBe(requestId);
        sut.ProviderName.ShouldBe("patients");
        sut.Action.ShouldBe(DeletionAction.PhysicalDelete);
        sut.AffectedRecords.ShouldBe(42);
        sut.Details.ShouldBe("All patient records deleted");
    }

    [Fact]
    public void Details_CanBeNull()
    {
        var sut = new PersonalDataDeletedEvent(
            Guid.NewGuid(),
            "billing",
            DeletionAction.Anonymized,
            5,
            null);

        sut.Details.ShouldBeNull();
    }

    [Theory]
    [InlineData(DeletionAction.PhysicalDelete)]
    [InlineData(DeletionAction.SoftDelete)]
    [InlineData(DeletionAction.Anonymized)]
    [InlineData(DeletionAction.Retained)]
    [InlineData(DeletionAction.Mixed)]
    public void Constructor_AcceptsAllDeletionActions(DeletionAction action)
    {
        var sut = new PersonalDataDeletedEvent(
            Guid.NewGuid(),
            "provider",
            action,
            1,
            null);

        sut.Action.ShouldBe(action);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var requestId = Guid.NewGuid();

        var a = new PersonalDataDeletedEvent(requestId, "p", DeletionAction.SoftDelete, 3, "info");
        var b = new PersonalDataDeletedEvent(requestId, "p", DeletionAction.SoftDelete, 3, "info");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new PersonalDataDeletedEvent(Guid.NewGuid(), "p", DeletionAction.SoftDelete, 3, null);
        var b = new PersonalDataDeletedEvent(Guid.NewGuid(), "q", DeletionAction.Retained, 1, null);

        a.ShouldNotBe(b);
    }
}
