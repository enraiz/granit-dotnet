// =============================================================================
// SettingDefinitionManagerTests - Tests unitaires du registre de définitions
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Settings.Tests;

public sealed class SettingDefinitionManagerTests
{
    private sealed class FakeProvider(params SettingDefinition[] definitions) : ISettingDefinitionProvider
    {
        public void Define(ISettingDefinitionContext context)
        {
            foreach (SettingDefinition def in definitions)
            {
                context.Add(def);
            }
        }
    }

    [Fact]
    public void NoProviders_Returns_EmptyCollection()
    {
        SettingDefinitionManager manager = new([]);

        manager.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Get_KnownSetting_Returns_Definition()
    {
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };
        SettingDefinitionManager manager = new([new FakeProvider(def)]);

        SettingDefinition result = manager.Get("App.Theme");

        result.Should().BeSameAs(def);
    }

    [Fact]
    public void Get_UnknownSetting_Throws_InvalidOperationException()
    {
        SettingDefinitionManager manager = new([]);

        Action act = () => manager.Get("Unknown.Setting");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown.Setting*");
    }

    [Fact]
    public void GetOrNull_UnknownSetting_Returns_Null()
    {
        SettingDefinitionManager manager = new([]);

        SettingDefinition? result = manager.GetOrNull("Unknown.Setting");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_Returns_AllDeclaredDefinitions()
    {
        SettingDefinition def1 = new("App.Theme");
        SettingDefinition def2 = new("App.Language");
        SettingDefinitionManager manager = new([new FakeProvider(def1, def2)]);

        IReadOnlyCollection<SettingDefinition> all = manager.GetAll();

        all.Should().HaveCount(2);
        all.Should().Contain(def1);
        all.Should().Contain(def2);
    }

    [Fact]
    public void LaterProvider_Overwrites_SameNameDefinition()
    {
        SettingDefinition first = new("App.Theme") { DefaultValue = "dark" };
        SettingDefinition second = new("App.Theme") { DefaultValue = "light" };
        FakeProvider providerA = new(first);
        FakeProvider providerB = new(second);

        SettingDefinitionManager manager = new([providerA, providerB]);

        manager.Get("App.Theme").DefaultValue.Should().Be("light", "le second provider écrase le premier");
    }

    [Fact]
    public void DefinitionContext_GetOrNull_Returns_AlreadyAddedDefinition()
    {
        SettingDefinition added = new("App.Theme");
        SettingDefinition? capturedFromContext = null;

        FakeProvider provider = new FakeProvider(added);

        // On utilise un provider qui consulte le contexte pendant Define()
        SettingDefinitionManager manager = new([
            new InspectingProvider(added, ctx =>
            {
                capturedFromContext = ctx.GetOrNull("App.Theme");
            })
        ]);

        capturedFromContext.Should().BeSameAs(added,
            "GetOrNull doit retrouver la définition ajoutée dans le même contexte");
    }

    private sealed class InspectingProvider(
        SettingDefinition definition,
        Action<ISettingDefinitionContext> inspect) : ISettingDefinitionProvider
    {
        public void Define(ISettingDefinitionContext context)
        {
            context.Add(definition);
            inspect(context);
        }
    }
}
