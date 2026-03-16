using System.Diagnostics;

namespace Granit.AI.Ollama.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.AI.Ollama distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class AIOllamaActivitySource
{
    /// <summary>The name of the Granit.AI.Ollama <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.AI.Ollama";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string CreateChatClient = "ai.ollama.create-chat-client";
    internal const string CreateEmbeddingGenerator = "ai.ollama.create-embedding-generator";

    // ──── Tag names ────

    internal const string TagModel = "ai.ollama.model";
    internal const string TagEndpoint = "ai.ollama.endpoint";
}
