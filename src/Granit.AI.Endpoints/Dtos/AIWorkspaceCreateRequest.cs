namespace Granit.AI.Endpoints.Dtos;

/// <summary>
/// Request to create a new dynamic AI workspace.
/// </summary>
/// <param name="Key">Machine key for this workspace (lowercase alphanumeric with hyphens, e.g. <c>support-chat</c>).</param>
/// <param name="Provider">Provider identifier (e.g. <c>OpenAI</c>, <c>AzureOpenAI</c>).</param>
/// <param name="Model">Model identifier (e.g. <c>gpt-4o</c>).</param>
/// <param name="DisplayName">
/// Optional short human-readable label (e.g. <c>GPT-4o</c>, <c>Support AI</c>). Max 64 chars.
/// When omitted, the model identifier is used as the display fallback.
/// </param>
/// <param name="SystemPrompt">Optional system prompt.</param>
/// <param name="Temperature">Optional sampling temperature (0.0–2.0).</param>
/// <param name="MaxOutputTokens">Optional maximum output tokens.</param>
public sealed record AIWorkspaceCreateRequest(
    string Key,
    string Provider,
    string Model,
    string? DisplayName = null,
    string? SystemPrompt = null,
    float? Temperature = null,
    int? MaxOutputTokens = null) : IAIWorkspaceMutableFields;
