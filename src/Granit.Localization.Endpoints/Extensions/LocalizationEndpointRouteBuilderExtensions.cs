// ---------------------------------------------------------------------------
// LocalizationEndpointRouteBuilderExtensions.cs
// Minimal API extension that exposes all registered Granit localization
// resources at GET /api/granit/localization?cultureName={culture}.
//
// Designed for SPA client bootstrapping (Angular, Blazor, React).
// Returns translations + available languages in a single HTTP call.
// ---------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Granit.Localization.Attributes;
using Granit.Localization.Endpoints.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Granit.Localization.Endpoints.Extensions;

/// <summary>
/// Extension methods for mapping Granit localization endpoints.
/// </summary>
public static class LocalizationEndpointRouteBuilderExtensions
{
    // BCP 47 language tag: 2-8 alpha primary subtag, optional hyphen-separated subtags (1-8 alphanumeric).
    private static readonly Regex Bcp47Pattern =
        new(@"^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{1,8})*$", RegexOptions.Compiled);

    /// <summary>
    /// Maps <c>GET /api/granit/localization</c> — returns all registered localization
    /// resources for the requested culture, plus the list of available languages.
    /// </summary>
    /// <remarks>
    /// <para>The endpoint is anonymous: translation strings are public UI data.</para>
    /// <para>
    /// Response headers include <c>Cache-Control: public, max-age=3600</c> and
    /// <c>Vary: Accept-Language</c> to allow browser and CDN caching per culture.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapGranitLocalization(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/api/granit/localization", HandleAsync)
            .AllowAnonymous()
            .WithName("GetGranitLocalization")
            .WithTags("Granit")
            .WithSummary("Returns all localization resources for the requested culture.");

        return endpoints;
    }

    private static IResult HandleAsync(HttpContext context, string? cultureName = null)
    {
        IOptions<GranitLocalizationOptions> options =
            context.RequestServices.GetRequiredService<IOptions<GranitLocalizationOptions>>();
        IStringLocalizerFactory localizerFactory =
            context.RequestServices.GetRequiredService<IStringLocalizerFactory>();

        if (!string.IsNullOrWhiteSpace(cultureName) && !Bcp47Pattern.IsMatch(cultureName))
        {
            return Results.Problem(
                detail: $"Culture name '{cultureName}' is not supported.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        CultureInfo culture = string.IsNullOrWhiteSpace(cultureName)
            ? CultureInfo.CurrentUICulture
            : CultureInfo.GetCultureInfo(cultureName);

        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        Dictionary<string, IReadOnlyDictionary<string, string>> resources;

        try
        {
            CultureInfo.CurrentUICulture = culture;
            resources = BuildResources(options.Value, localizerFactory);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }

        List<LanguageInfoDto> languages = [.. options.Value.Languages
            .Select(l => new LanguageInfoDto(l.CultureName, l.DisplayName, l.FlagIcon))];

        context.Response.Headers.CacheControl = "public, max-age=3600";
        context.Response.Headers.Vary = "Accept-Language";

        return Results.Ok(new ApplicationLocalizationDto(culture.Name, resources, languages));
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> BuildResources(
        GranitLocalizationOptions options,
        IStringLocalizerFactory localizerFactory)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> resources = new(StringComparer.Ordinal);

        foreach (LocalizationResourceInfo resourceInfo in options.Resources.GetAll())
        {
            string name = resourceInfo.ResourceType
                .GetCustomAttribute<LocalizationResourceNameAttribute>()?.Name
                ?? resourceInfo.ResourceType.Name;

            IStringLocalizer localizer = localizerFactory.Create(resourceInfo.ResourceType);

            Dictionary<string, string> translations = localizer
                .GetAllStrings(includeParentCultures: true)
                .ToDictionary(s => s.Name, s => s.Value, StringComparer.Ordinal);

            resources[name] = translations;
        }

        return resources;
    }
}
