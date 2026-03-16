using System.Diagnostics;

namespace Granit.AI.OpenAI.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.AI.OpenAI distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class AIOpenAIActivitySource
{
    /// <summary>The name of the Granit.AI.OpenAI <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.AI.OpenAI";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);
}
