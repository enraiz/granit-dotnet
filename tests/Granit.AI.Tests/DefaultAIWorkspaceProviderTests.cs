using Granit.AI.Internal;
using Granit.AI.Workspaces;
using NSubstitute;
using Shouldly;

namespace Granit.AI.Tests;

public sealed class DefaultAIWorkspaceProviderTests
{
    private readonly IAIWorkspaceStoreReader _storeReader = Substitute.For<IAIWorkspaceStoreReader>();

    private static AIWorkspace CreateWorkspace(string key, AIWorkspaceKind kind = AIWorkspaceKind.Dynamic) =>
        new()
        {
            Key = key,
            Provider = "OpenAI",
            Model = "gpt-4o",
            Kind = kind,
        };

    [Fact]
    public async Task GetAsync_SystemWorkspace_ReturnedWithoutHittingStore()
    {
        IAIWorkspaceDefinitionProvider definitionProvider = Substitute.For<IAIWorkspaceDefinitionProvider>();
        definitionProvider.When(p => p.Define(Arg.Any<IAIWorkspaceDefinitionContext>()))
            .Do(call => call.Arg<IAIWorkspaceDefinitionContext>().Add(CreateWorkspace("system-ws")));

        var provider = new DefaultAIWorkspaceProvider([definitionProvider], _storeReader);

        AIWorkspace? result = await provider.GetAsync("system-ws", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Key.ShouldBe("system-ws");
        result.Kind.ShouldBe(AIWorkspaceKind.System);
        await _storeReader.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_DynamicWorkspace_FallsBackToStore()
    {
        AIWorkspace workspace = CreateWorkspace("dynamic-ws");
        _storeReader.FindAsync("dynamic-ws", Arg.Any<CancellationToken>()).Returns(workspace);

        var provider = new DefaultAIWorkspaceProvider([], _storeReader);

        AIWorkspace? result = await provider.GetAsync("dynamic-ws", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Key.ShouldBe("dynamic-ws");
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        _storeReader.FindAsync("missing", Arg.Any<CancellationToken>()).Returns((AIWorkspace?)null);

        var provider = new DefaultAIWorkspaceProvider([], _storeReader);

        AIWorkspace? result = await provider.GetAsync("missing", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_MergesSystemAndDynamic_SystemTakesPrecedence()
    {
        IAIWorkspaceDefinitionProvider definitionProvider = Substitute.For<IAIWorkspaceDefinitionProvider>();
        definitionProvider.When(p => p.Define(Arg.Any<IAIWorkspaceDefinitionContext>()))
            .Do(call => call.Arg<IAIWorkspaceDefinitionContext>().Add(CreateWorkspace("shared")));

        _storeReader.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([CreateWorkspace("shared"), CreateWorkspace("dynamic-only")]);

        var provider = new DefaultAIWorkspaceProvider([definitionProvider], _storeReader);

        IReadOnlyList<AIWorkspace> result = await provider.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(w => w.Key == "shared" && w.Kind == AIWorkspaceKind.System);
        result.ShouldContain(w => w.Key == "dynamic-only");
    }
}
