using Granit.AI.Anthropic.Extensions;
using Granit.Core.Modularity;
using Microsoft.Extensions.Hosting;

namespace Granit.AI.Anthropic;

/// <summary>
/// Granit module for the Anthropic (Claude) AI provider.
/// </summary>
/// <remarks>
/// Registers <see cref="IAIProviderFactory"/> for Anthropic, enabling
/// Claude models (Opus, Sonnet, Haiku) as chat completion providers.
/// Anthropic does not support embedding generation.
/// </remarks>
[DependsOn(typeof(GranitAIModule))]
public sealed class GranitAIAnthropicModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitAIAnthropic();
}
