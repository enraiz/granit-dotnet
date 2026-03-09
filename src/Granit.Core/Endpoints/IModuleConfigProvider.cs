namespace Granit.Core.Endpoints;

/// <summary>
/// Provides a read-only snapshot of a module's configuration for client consumption.
/// </summary>
/// <typeparam name="TResponse">
/// The response DTO type exposed via <c>GET /{module}/config</c>.
/// Must be a <c>sealed record</c> following the <c>*Response</c> naming convention.
/// </typeparam>
/// <remarks>
/// <para>
/// Implement this interface in each module that needs to expose its <c>IOptions&lt;T&gt;</c>
/// configuration to frontend clients. The implementation maps internal options to a
/// public-facing DTO — never expose the raw options class.
/// </para>
/// <para>
/// Register via <c>AddModuleConfig&lt;TProvider, TResponse&gt;()</c> and map via
/// <c>MapGranitModuleConfig&lt;TProvider, TResponse&gt;()</c>.
/// </para>
/// <example>
/// <code>
/// internal sealed class WebhookModuleConfigProvider(IOptions&lt;WebhooksOptions&gt; options)
///     : IModuleConfigProvider&lt;WebhookModuleConfigResponse&gt;
/// {
///     public WebhookModuleConfigResponse GetConfig() =&gt;
///         new(options.Value.StorePayload);
/// }
/// </code>
/// </example>
/// </remarks>
public interface IModuleConfigProvider<out TResponse> where TResponse : class
{
    /// <summary>
    /// Returns the current module configuration as a public-facing DTO.
    /// </summary>
    TResponse GetConfig();
}
