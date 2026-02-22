using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.ExceptionHandling.Extensions;

namespace DigitalDynamics.Foundation.ExceptionHandling;

/// <summary>
/// Foundation module for centralized exception handling.
/// Registers <c>AddFoundationExceptionHandling()</c>.
/// </summary>
/// <remarks>
/// Call <c>app.UseFoundationExceptionHandling()</c> in <c>Program.cs</c>
/// <b>as the first middleware</b>, before routing and authentication.
/// </remarks>
public sealed class FoundationExceptionHandlingModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationExceptionHandling();
}
