// ---------------------------------------------------------------------------
// LocalizationEndpointRouteBuilderExtensions.cs
// Minimal API extensions for Granit localization:
//   - MapGranitLocalization: GET /{prefix}/localization (SPA bootstrapping, anonymous)
//   - MapGranitLocalizationOverrides: CRUD /{prefix}/localization/overrides
//     (admin, requires Localization.Overrides.Manage permission)
// ---------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Granit.Core.Localization;
using Granit.Localization.Endpoints.Dtos;
using Granit.Localization.Endpoints.Permissions;
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
public static partial class LocalizationEndpointRouteBuilderExtensions
{
    // BCP 47 language tag: 2-8 alpha primary subtag, optional hyphen-separated subtags (1-8 alphanumeric).
    [GeneratedRegex(@"^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{1,8})*$")]
    private static partial Regex Bcp47Pattern();

    // Column size constraints (must match LocalizationOverrideConfiguration).
    private const int MaxResourceNameLength = 200;
    private const int MaxKeyLength = 500;
    private const int MaxValueLength = 4000;

    /// <summary>
    /// Maps <c>GET /{prefix}/localization</c> — returns all registered localization
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
    /// <param name="configure">Optional delegate to customize <see cref="LocalizationEndpointsOptions"/>.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapGranitLocalization(
        this IEndpointRouteBuilder endpoints,
        Action<LocalizationEndpointsOptions>? configure = null)
    {
        LocalizationEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        endpoints
            .MapGet(prefix, HandleGetLocalizationAsync)
            .AllowAnonymous()
            .WithName("GetGranitLocalization")
            .WithTags(options.TagName)
            .WithSummary("Returns all localization resources for the requested culture.");

        return endpoints;
    }

    /// <summary>
    /// Maps the localization override management endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers 3 endpoints under <c>/{prefix}/localization/overrides</c>:
    /// <list type="bullet">
    /// <item><c>GET ?resourceName=X&amp;cultureName=fr</c> — list all overrides for a resource/culture</item>
    /// <item><c>PUT /{resourceName}/{cultureName}/{key}</c> — create or update an override</item>
    /// <item><c>DELETE /{resourceName}/{cultureName}/{key}</c> — remove an override</item>
    /// </list>
    /// </para>
    /// <para>
    /// All endpoints require the <c>Localization.Overrides.Manage</c> permission.
    /// If <see cref="ILocalizationOverrideStore"/> is not registered (no EF Core or other
    /// persistence module loaded), all endpoints return <c>501 Not Implemented</c>.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="LocalizationEndpointsOptions"/>.</param>
    /// <returns>The route group builder for further chaining.</returns>
    public static RouteGroupBuilder MapGranitLocalizationOverrides(
        this IEndpointRouteBuilder endpoints,
        Action<LocalizationEndpointsOptions>? configure = null)
    {
        LocalizationEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        RouteGroupBuilder group = endpoints
            .MapGroup($"{prefix}/overrides")
            .RequireAuthorization(LocalizationOverridesPermissions.Manage)
            .WithTags(options.TagName);

        group.MapGet("", HandleGetOverridesAsync)
             .WithName("GetLocalizationOverrides")
             .WithSummary("Returns all translation overrides for a resource and culture.");

        group.MapPut("/{resourceName}/{cultureName}/{key}", HandlePutOverrideAsync)
             .WithName("PutLocalizationOverride")
             .WithSummary("Creates or updates a translation override.");

        group.MapDelete("/{resourceName}/{cultureName}/{key}", HandleDeleteOverrideAsync)
             .WithName("DeleteLocalizationOverride")
             .WithSummary("Removes a translation override.");

        return group;
    }

    // -------------------------------------------------------------------------
    // Handlers — GET /{prefix}/localization
    // -------------------------------------------------------------------------

    private static IResult HandleGetLocalizationAsync(HttpContext context, string? cultureName = null)
    {
        IOptions<GranitLocalizationOptions> options =
            context.RequestServices.GetRequiredService<IOptions<GranitLocalizationOptions>>();
        IStringLocalizerFactory localizerFactory =
            context.RequestServices.GetRequiredService<IStringLocalizerFactory>();

        if (!string.IsNullOrWhiteSpace(cultureName) && !Bcp47Pattern().IsMatch(cultureName))
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

        List<LanguageInfoResponse> languages = [.. options.Value.Languages
            .Select(l => new LanguageInfoResponse(l.CultureName, l.DisplayName, l.FlagIcon, l.IsDefault))];

        context.Response.Headers.CacheControl = "public, max-age=3600";
        context.Response.Headers.Vary = "Accept-Language";

        return Results.Ok(new ApplicationLocalizationResponse(culture.Name, resources, languages));
    }

    // -------------------------------------------------------------------------
    // Handlers — CRUD /{prefix}/localization/overrides
    // -------------------------------------------------------------------------

    private static async Task<IResult> HandleGetOverridesAsync(
        HttpContext context,
        string? resourceName,
        string? cultureName,
        CancellationToken ct)
    {
        ILocalizationOverrideStore? store =
            context.RequestServices.GetService<ILocalizationOverrideStore>();

        if (store is null)
        {
            return Results.Problem(
                detail: "No ILocalizationOverrideStore is registered. Add GranitLocalizationDatabaseSourceEntityFrameworkCoreModule.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        if (string.IsNullOrWhiteSpace(resourceName) || string.IsNullOrWhiteSpace(cultureName))
        {
            return Results.Problem(
                detail: "Query parameters 'resourceName' and 'cultureName' are required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (resourceName.Length > MaxResourceNameLength)
        {
            return Results.Problem(
                detail: $"resourceName must not exceed {MaxResourceNameLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Bcp47Pattern().IsMatch(cultureName))
        {
            return Results.Problem(
                detail: $"Culture name '{cultureName}' is not a valid BCP 47 tag.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        IReadOnlyDictionary<string, string> overrides =
            await store.GetOverridesAsync(resourceName, cultureName, ct);

        return Results.Ok(overrides);
    }

    private static async Task<IResult> HandlePutOverrideAsync(
        HttpContext context,
        string resourceName,
        string cultureName,
        string key,
        SetLocalizationOverrideRequest body,
        CancellationToken ct)
    {
        ILocalizationOverrideStore? store =
            context.RequestServices.GetService<ILocalizationOverrideStore>();

        if (store is null)
        {
            return Results.Problem(
                detail: "No ILocalizationOverrideStore is registered. Add GranitLocalizationDatabaseSourceEntityFrameworkCoreModule.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        if (resourceName.Length > MaxResourceNameLength)
        {
            return Results.Problem(
                detail: $"resourceName must not exceed {MaxResourceNameLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Bcp47Pattern().IsMatch(cultureName))
        {
            return Results.Problem(
                detail: $"Culture name '{cultureName}' is not a valid BCP 47 tag.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (key.Length > MaxKeyLength)
        {
            return Results.Problem(
                detail: $"key must not exceed {MaxKeyLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(body.Value))
        {
            return Results.Problem(
                detail: "Override value must not be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (body.Value.Length > MaxValueLength)
        {
            return Results.Problem(
                detail: $"value must not exceed {MaxValueLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await store.SetOverrideAsync(resourceName, cultureName, key, body.Value, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleDeleteOverrideAsync(
        HttpContext context,
        string resourceName,
        string cultureName,
        string key,
        CancellationToken ct)
    {
        ILocalizationOverrideStore? store =
            context.RequestServices.GetService<ILocalizationOverrideStore>();

        if (store is null)
        {
            return Results.Problem(
                detail: "No ILocalizationOverrideStore is registered. Add GranitLocalizationDatabaseSourceEntityFrameworkCoreModule.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        if (resourceName.Length > MaxResourceNameLength)
        {
            return Results.Problem(
                detail: $"resourceName must not exceed {MaxResourceNameLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Bcp47Pattern().IsMatch(cultureName))
        {
            return Results.Problem(
                detail: $"Culture name '{cultureName}' is not a valid BCP 47 tag.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (key.Length > MaxKeyLength)
        {
            return Results.Problem(
                detail: $"key must not exceed {MaxKeyLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await store.RemoveOverrideAsync(resourceName, cultureName, key, ct);
        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static Dictionary<string, IReadOnlyDictionary<string, string>> BuildResources(
        GranitLocalizationOptions options,
        IStringLocalizerFactory localizerFactory)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> resources = new(StringComparer.Ordinal);

        foreach (Type resourceType in options.Resources.GetAll().Select(resourceInfo => resourceInfo.ResourceType))
        {
            string name = resourceType
                .GetCustomAttribute<LocalizationResourceNameAttribute>()?.Name
                ?? resourceType.Name;

            IStringLocalizer localizer = localizerFactory.Create(resourceType);

            Dictionary<string, string> translations = localizer
                .GetAllStrings(includeParentCultures: true)
                .ToDictionary(s => s.Name, s => s.Value, StringComparer.Ordinal);

            resources[name] = translations;
        }

        return resources;
    }
}
