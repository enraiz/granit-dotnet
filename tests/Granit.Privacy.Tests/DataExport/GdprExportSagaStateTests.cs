using Granit.Privacy.DataExport;
using Shouldly;
using Xunit;

namespace Granit.Privacy.Tests.DataExport;

public sealed class GdprExportSagaStateTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var sut = new GdprExportSagaState();

        sut.RequestId.ShouldBe(Guid.Empty);
        sut.UserId.ShouldBe(Guid.Empty);
        sut.ExpectedCount.ShouldBe(0);
        sut.ReceivedFragments.ShouldBeEmpty();
        sut.AllProviders.ShouldBeEmpty();
        sut.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sut = new GdprExportSagaState
        {
            RequestId = requestId,
            UserId = userId,
            ExpectedCount = 3,
            IsCompleted = true,
            AllProviders = ["patients", "billing", "appointments"]
        };

        sut.RequestId.ShouldBe(requestId);
        sut.UserId.ShouldBe(userId);
        sut.ExpectedCount.ShouldBe(3);
        sut.IsCompleted.ShouldBeTrue();
        sut.AllProviders.Count.ShouldBe(3);
    }

    [Fact]
    public void ReceivedFragments_CanBePopulated()
    {
        var sut = new GdprExportSagaState();

        var fragment = new ReceivedFragment("patients", "blob-ref-123", "application/json");
        sut.ReceivedFragments.Add(fragment);

        sut.ReceivedFragments.Count.ShouldBe(1);
        sut.ReceivedFragments[0].ShouldBe(fragment);
    }

    [Fact]
    public void ReceivedFragment_Constructor_SetsAllProperties()
    {
        var sut = new ReceivedFragment("billing", "blob-456", "text/csv");

        sut.ProviderName.ShouldBe("billing");
        sut.BlobReferenceId.ShouldBe("blob-456");
        sut.ContentType.ShouldBe("text/csv");
    }

    [Fact]
    public void ReceivedFragment_Equality_SameValues_AreEqual()
    {
        var a = new ReceivedFragment("p", "ref", "application/json");
        var b = new ReceivedFragment("p", "ref", "application/json");

        a.ShouldBe(b);
    }
}
