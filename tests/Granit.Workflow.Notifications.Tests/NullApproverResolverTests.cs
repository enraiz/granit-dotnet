using Granit.Workflow.Notifications.Internal;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class NullApproverResolverTests
{
    [Fact]
    public async Task ResolveApproversAsync_ReturnsEmptyList()
    {
        NullApproverResolver resolver = new();

        IReadOnlyList<string> result = await resolver.ResolveApproversAsync(
            "any.permission", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Implements_IApproverResolver()
    {
        typeof(NullApproverResolver)
            .IsAssignableTo(typeof(IApproverResolver))
            .ShouldBeTrue();
    }

    [Fact]
    public void Class_IsSealed_AndInternal()
    {
        typeof(NullApproverResolver).IsSealed.ShouldBeTrue();
        typeof(NullApproverResolver).IsNotPublic.ShouldBeTrue();
    }
}
