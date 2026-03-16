using System.Diagnostics;

namespace Granit.AI.AzureOpenAI.Diagnostics;

/// <summary>
/// OpenTelemetry activity source for the Azure OpenAI provider.
/// </summary>
internal static class AIAzureOpenAIActivitySource
{
    public const string Name = "Granit.AI.AzureOpenAI";

    internal static readonly ActivitySource Instance = new(Name);
}
