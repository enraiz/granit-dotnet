using Granit.Core.Modularity;
using Granit.ExceptionHandling;
using Granit.Localization;
using Granit.Localization.Options;
using Granit.Validation.Extensions;
using Granit.Validation.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Validation;

/// <summary>
/// Granit module for input validation.
/// </summary>
/// <remarks>
/// Registers <c>AddGranitValidation()</c>, which configures FluentValidation to
/// emit structured error codes (<c>Granit:Validation:*</c>) instead of
/// human-readable messages. The SPA resolves codes from its local localization
/// dictionary served by <c>GET /api/granit/localization</c>.
/// </remarks>
[DependsOn(typeof(GranitExceptionHandlingModule))]
[DependsOn(typeof(GranitLocalizationModule))]
public sealed class GranitValidationModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitValidation();

        context.Services.AddSingleton<IExceptionStatusCodeMapper, FluentValidationExceptionStatusCodeMapper>();

        context.Services.Configure<GranitLocalizationOptions>(options =>
        {
            options.Resources
                .Add<ValidationLocalizationResource>("fr")
                .AddJson(
                    typeof(ValidationLocalizationResource).Assembly,
                    "Granit.Validation.Localization.Validation");
        });
    }
}
