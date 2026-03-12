// =============================================================================
// Tests - ClaimCheckReference
// =============================================================================
// Verifies ClaimCheckReference record behavior.
// =============================================================================

using Granit.Wolverine.ClaimCheck;
using Shouldly;
using Xunit;

namespace Granit.Wolverine.Tests.ClaimCheck;

public sealed class ClaimCheckReferenceTests
{
    [Fact]
    public void Create_SetsPayloadTypeFromGenericParameter()
    {
        var id = Guid.NewGuid();

        var reference = ClaimCheckReference.Create<SampleMessage>(id);

        reference.ReferenceId.ShouldBe(id);
        reference.PayloadType.ShouldContain(nameof(SampleMessage));
        reference.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void Constructor_AllowsCustomContentType()
    {
        var reference = new ClaimCheckReference(Guid.NewGuid(), "MyType", "application/xml");

        reference.ContentType.ShouldBe("application/xml");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var r1 = new ClaimCheckReference(id, "Type1");
        var r2 = new ClaimCheckReference(id, "Type1");
        var r3 = new ClaimCheckReference(Guid.NewGuid(), "Type1");

        r1.ShouldBe(r2);
        r1.ShouldNotBe(r3);
    }

    private sealed record SampleMessage(string Data);
}
