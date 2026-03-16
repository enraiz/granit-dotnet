using Granit.AI.Internal;
using Granit.AI.Workspaces;
using Shouldly;

namespace Granit.AI.Tests;

public sealed class AIWorkspaceDefinitionContextTests
{
    private static AIWorkspace CreateWorkspace(string name) =>
        new()
        {
            Name = name,
            Provider = "OpenAI",
            Model = "gpt-4o",
        };

    [Fact]
    public void Add_ForcesKindToSystem()
    {
        var context = new AIWorkspaceDefinitionContext();

        context.Add(CreateWorkspace("ws") with { Kind = AIWorkspaceKind.Dynamic });

        context.Workspaces["ws"].Kind.ShouldBe(AIWorkspaceKind.System);
    }

    [Fact]
    public void Add_DuplicateName_Throws()
    {
        var context = new AIWorkspaceDefinitionContext();
        context.Add(CreateWorkspace("ws"));

        Should.Throw<InvalidOperationException>(() => context.Add(CreateWorkspace("ws")));
    }

    [Fact]
    public void Add_CaseInsensitiveDuplicate_Throws()
    {
        var context = new AIWorkspaceDefinitionContext();
        context.Add(CreateWorkspace("MyWorkspace"));

        Should.Throw<InvalidOperationException>(() => context.Add(CreateWorkspace("myworkspace")));
    }
}
