// ---------------------------------------------------------------------------
// TemplatingEndpointRouteBuilderExtensions.cs
// Minimal API extensions for Granit template administration:
//   - MapGranitTemplatingAdmin: CRUD endpoints for template draft management
//     (requires Templates.Manage permission)
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Granit.Security;
using Granit.Templating.Endpoints.Dtos;
using Granit.Templating.Endpoints.Permissions;
using Granit.Templating.Exceptions;
using Granit.Templating.GlobalContext;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Templating.Endpoints.Extensions;

/// <summary>
/// Extension methods for mapping Granit template administration endpoints.
/// </summary>
public static partial class TemplatingEndpointRouteBuilderExtensions
{
    // BCP 47 language tag: 2-8 alpha primary subtag, optional hyphen-separated subtags.
    [GeneratedRegex(@"^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{1,8})*$")]
    private static partial Regex Bcp47Pattern();

    // Template name: "Domain.Name" pattern.
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$")]
    private static partial Regex TemplateNamePattern();

    private const int MaxNameLength = 200;
    private const int MaxCultureLength = 10;

    /// <summary>
    /// Maps template administration endpoints under <c>/{prefix}</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers 11 endpoints:
    /// <list type="bullet">
    /// <item><c>GET /</c> — paginated list with filters</item>
    /// <item><c>GET /{name}</c> — detail (draft + published)</item>
    /// <item><c>POST /</c> — create a new draft</item>
    /// <item><c>PUT /{name}</c> — update an existing draft</item>
    /// <item><c>DELETE /{name}/draft</c> — delete draft only</item>
    /// <item><c>POST /{name}/publish</c> — publish the current draft</item>
    /// <item><c>POST /{name}/unpublish</c> — unpublish (archive the published revision)</item>
    /// <item><c>GET /{name}/lifecycle</c> — lifecycle info (current status, available transitions)</item>
    /// <item><c>POST /{name}/preview</c> — render the current draft with test data</item>
    /// <item><c>GET /{name}/history</c> — paginated revision history (summaries, no content)</item>
    /// <item><c>GET /{name}/history/{revisionId}</c> — full detail of a specific revision</item>
    /// </list>
    /// </para>
    /// <para>
    /// All endpoints require the <c>Templates.Manage</c> permission.
    /// If <see cref="IDocumentTemplateStoreReader"/>/<see cref="IDocumentTemplateStoreWriter"/> is not registered
    /// (no EF Core persistence module loaded), all endpoints return <c>501 Not Implemented</c>.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="TemplatingEndpointsOptions"/>.</param>
    /// <returns>The route group builder for further chaining.</returns>
    public static RouteGroupBuilder MapGranitTemplatingAdmin(
        this IEndpointRouteBuilder endpoints,
        Action<TemplatingEndpointsOptions>? configure = null)
    {
        TemplatingEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        RouteGroupBuilder group = endpoints
            .MapGroup(prefix)
            .RequireAuthorization(TemplatingPermissions.Manage)
            .WithTags(options.TagName);

        group.MapGet("/", HandleListAsync)
             .WithName("ListTemplates")
             .WithSummary("Returns a paginated list of templates with filters.");

        group.MapGet("/{name}", HandleGetDetailAsync)
             .WithName("GetTemplateDetail")
             .WithSummary("Returns detail of a template (current draft and published revision).");

        group.MapPost("/", HandleCreateAsync)
             .WithName("CreateTemplateDraft")
             .WithSummary("Creates a new template draft.")
             .ValidateBody<SaveTemplateRequest>();

        group.MapPut("/{name}", HandleUpdateAsync)
             .WithName("UpdateTemplateDraft")
             .WithSummary("Updates an existing template draft.")
             .ValidateBody<SaveTemplateRequest>();

        group.MapDelete("/{name}/draft", HandleDeleteDraftAsync)
             .WithName("DeleteTemplateDraft")
             .WithSummary("Deletes the draft revision of a template (published/archived are preserved).");

        group.MapPost("/{name}/publish", HandlePublishAsync)
             .WithName("PublishTemplate")
             .WithSummary("Publishes the current draft, archiving any previous published revision.");

        group.MapPost("/{name}/unpublish", HandleUnpublishAsync)
             .WithName("UnpublishTemplate")
             .WithSummary("Unpublishes the template (archives the published revision).");

        group.MapGet("/{name}/lifecycle", HandleGetLifecycleAsync)
             .WithName("GetTemplateLifecycle")
             .WithSummary("Returns lifecycle status, workflow state, and available transitions.");

        group.MapPost("/{name}/preview", HandlePreviewAsync)
             .WithName("PreviewTemplate")
             .WithSummary("Renders the current draft with optional test data and returns the HTML output.");

        group.MapGet("/{name}/history", HandleGetHistoryAsync)
             .WithName("GetTemplateHistory")
             .WithSummary("Returns a paginated revision history for the template (without content).");

        group.MapGet("/{name}/history/{revisionId:guid}", HandleGetRevisionDetailAsync)
             .WithName("GetTemplateRevisionDetail")
             .WithSummary("Returns the full detail of a specific template revision (including content).");

        return group;
    }

    // -------------------------------------------------------------------------
    // GET / — List templates
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateListResponse>, ProblemHttpResult>> HandleListAsync(
        HttpContext context,
        [AsParameters] TemplateListQueryParameters parameters,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? error = ValidatePagination(parameters.Page, parameters.PageSize);
        if (error is not null)
        {
            return error;
        }

        if (parameters.Culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(parameters.Culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        TemplateListFilter filter = new(
            Page: parameters.Page,
            PageSize: parameters.PageSize,
            Search: parameters.Search,
            Status: parameters.Status,
            Culture: parameters.Culture);

        PagedTemplateResult result = await storeReader.ListTemplatesAsync(filter, ct).ConfigureAwait(false);

        List<TemplateListItemResponse> items = result.Items
            .Select(s => new TemplateListItemResponse(
                s.Name,
                s.Culture,
                s.MimeType,
                s.CurrentStatus,
                s.LastModifiedAt,
                s.LastModifiedBy,
                s.HasPublishedVersion))
            .ToList();

        return TypedResults.Ok(new TemplateListResponse(items, result.TotalCount));
    }

    // -------------------------------------------------------------------------
    // GET /{name} — Template detail
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateDetailResponse>, NotFound, ProblemHttpResult>> HandleGetDetailAsync(
        HttpContext context,
        string name,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        TemplateKey key = new(name, culture);

        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);
        Pipeline.TemplateDescriptor? published = await storeReader.TryGetPublishedAsync(key, ct).ConfigureAwait(false);

        if (draft is null && published is null)
        {
            return TypedResults.NotFound();
        }

        // For published, we need the full revision to build the response.
        // TryGetPublishedAsync returns a TemplateDescriptor (without metadata).
        // Use GetHistoryAsync to find the published revision with full metadata.
        TemplateRevisionResponse? publishedResponse = null;
        if (published is not null)
        {
            IReadOnlyList<TemplateRevision> history = await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);
            TemplateRevision? publishedRevision = history.FirstOrDefault(
                r => r.Status == TemplateLifecycleStatus.Published);

            if (publishedRevision is not null)
            {
                publishedResponse = ToRevisionResponse(publishedRevision);
            }
        }

        return TypedResults.Ok(new TemplateDetailResponse(
            name,
            culture,
            draft is not null ? ToRevisionResponse(draft) : null,
            publishedResponse));
    }

    // -------------------------------------------------------------------------
    // POST / — Create draft
    // -------------------------------------------------------------------------

    private static async Task<Results<Created<TemplateDetailResponse>, ProblemHttpResult>> HandleCreateAsync(
        HttpContext context,
        SaveTemplateRequest body,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();
        IDocumentTemplateStoreWriter? storeWriter =
            context.RequestServices.GetService<IDocumentTemplateStoreWriter>();

        if (storeReader is null || storeWriter is null)
        {
            return StoreNotRegistered();
        }

        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return TypedResults.Problem(
                detail: "Template name is required when creating a new template.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        ProblemHttpResult? nameError = ValidateTemplateName(body.Name);
        if (nameError is not null)
        {
            return nameError;
        }

        string userId = GetCurrentUserId(context);
        TemplateKey key = new(body.Name, body.Culture);

        await storeWriter.SaveDraftAsync(key, body.Content, body.MimeType, userId, ct).ConfigureAwait(false);

        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);
        Pipeline.TemplateDescriptor? published = await storeReader.TryGetPublishedAsync(key, ct).ConfigureAwait(false);

        TemplateRevisionResponse? publishedResponse = null;
        if (published is not null)
        {
            IReadOnlyList<TemplateRevision> history = await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);
            TemplateRevision? publishedRevision = history.FirstOrDefault(
                r => r.Status == TemplateLifecycleStatus.Published);

            if (publishedRevision is not null)
            {
                publishedResponse = ToRevisionResponse(publishedRevision);
            }
        }

        var response = new TemplateDetailResponse(
            body.Name,
            body.Culture,
            draft is not null ? ToRevisionResponse(draft) : null,
            publishedResponse);

        return TypedResults.Created($"{body.Name}", response);
    }

    // -------------------------------------------------------------------------
    // PUT /{name} — Update draft
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateDetailResponse>, ProblemHttpResult>> HandleUpdateAsync(
        HttpContext context,
        string name,
        SaveTemplateRequest body,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();
        IDocumentTemplateStoreWriter? storeWriter =
            context.RequestServices.GetService<IDocumentTemplateStoreWriter>();

        if (storeReader is null || storeWriter is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        string userId = GetCurrentUserId(context);
        TemplateKey key = new(name, body.Culture);

        await storeWriter.SaveDraftAsync(key, body.Content, body.MimeType, userId, ct).ConfigureAwait(false);

        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);
        Pipeline.TemplateDescriptor? published = await storeReader.TryGetPublishedAsync(key, ct).ConfigureAwait(false);

        TemplateRevisionResponse? publishedResponse = null;
        if (published is not null)
        {
            IReadOnlyList<TemplateRevision> history = await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);
            TemplateRevision? publishedRevision = history.FirstOrDefault(
                r => r.Status == TemplateLifecycleStatus.Published);

            if (publishedRevision is not null)
            {
                publishedResponse = ToRevisionResponse(publishedRevision);
            }
        }

        return TypedResults.Ok(new TemplateDetailResponse(
            name,
            body.Culture,
            draft is not null ? ToRevisionResponse(draft) : null,
            publishedResponse));
    }

    // -------------------------------------------------------------------------
    // DELETE /{name}/draft — Delete draft
    // -------------------------------------------------------------------------

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleDeleteDraftAsync(
        HttpContext context,
        string name,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreWriter? storeWriter =
            context.RequestServices.GetService<IDocumentTemplateStoreWriter>();

        if (storeWriter is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        string userId = GetCurrentUserId(context);
        TemplateKey key = new(name, culture);

        try
        {
            await storeWriter.DeleteDraftAsync(key, userId, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.NoContent();
    }

    // -------------------------------------------------------------------------
    // POST /{name}/publish — Publish the current draft
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateDetailResponse>, ProblemHttpResult>> HandlePublishAsync(
        HttpContext context,
        string name,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();
        IDocumentTemplateStoreWriter? storeWriter =
            context.RequestServices.GetService<IDocumentTemplateStoreWriter>();

        if (storeReader is null || storeWriter is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        string userId = GetCurrentUserId(context);
        TemplateKey key = new(name, culture);

        try
        {
            await storeWriter.PublishAsync(key, userId, ct).ConfigureAwait(false);
        }
        catch (TemplateTransitionDeniedException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(await BuildDetailResponseAsync(storeReader, key, ct).ConfigureAwait(false));
    }

    // -------------------------------------------------------------------------
    // POST /{name}/unpublish — Unpublish (archive the published revision)
    // -------------------------------------------------------------------------

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleUnpublishAsync(
        HttpContext context,
        string name,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreWriter? storeWriter =
            context.RequestServices.GetService<IDocumentTemplateStoreWriter>();

        if (storeWriter is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        string userId = GetCurrentUserId(context);
        TemplateKey key = new(name, culture);

        try
        {
            await storeWriter.UnpublishAsync(key, userId, ct).ConfigureAwait(false);
        }
        catch (TemplateTransitionDeniedException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.NoContent();
    }

    // -------------------------------------------------------------------------
    // GET /{name}/lifecycle — Lifecycle status and available transitions
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateLifecycleResponse>, NotFound, ProblemHttpResult>> HandleGetLifecycleAsync(
        HttpContext context,
        string name,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        TemplateKey key = new(name, culture);

        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);
        Pipeline.TemplateDescriptor? published = await storeReader.TryGetPublishedAsync(key, ct).ConfigureAwait(false);

        if (draft is null && published is null)
        {
            return TypedResults.NotFound();
        }

        // Determine the current status (Draft takes precedence for display)
        TemplateLifecycleStatus currentStatus = draft is not null
            ? TemplateLifecycleStatus.Draft
            : TemplateLifecycleStatus.Published;

        ITemplateTransitionHook? hook = context.RequestServices.GetService<ITemplateTransitionHook>();
        bool workflowEnabled = hook?.IsWorkflowEnabled ?? false;

        // Compute available transitions from the current status
        List<TemplateLifecycleStatus> availableTransitions = [];
        TemplateLifecycleStatus[] possibleTargets =
        [
            TemplateLifecycleStatus.Draft,
            TemplateLifecycleStatus.PendingReview,
            TemplateLifecycleStatus.Published,
            TemplateLifecycleStatus.Archived,
        ];

        foreach (TemplateLifecycleStatus target in possibleTargets)
        {
            if (target == currentStatus)
            {
                continue;
            }

            if (hook is not null &&
                await hook.CanTransitionAsync(currentStatus, target, ct).ConfigureAwait(false))
            {
                availableTransitions.Add(target);
            }
        }

        return TypedResults.Ok(new TemplateLifecycleResponse(
            name,
            culture,
            currentStatus,
            workflowEnabled,
            availableTransitions));
    }

    // -------------------------------------------------------------------------
    // GET /{name}/history — Paginated revision history (summaries)
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateHistoryResponse>, ProblemHttpResult>> HandleGetHistoryAsync(
        HttpContext context,
        string name,
        string? culture,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        ProblemHttpResult? paginationError = ValidatePagination(page, pageSize);
        if (paginationError is not null)
        {
            return paginationError;
        }

        TemplateKey key = new(name, culture);
        IReadOnlyList<TemplateRevision> allRevisions =
            await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);

        int totalCount = allRevisions.Count;
        List<TemplateRevisionSummaryResponse> summaries = allRevisions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new TemplateRevisionSummaryResponse(
                r.RevisionId,
                r.Status,
                r.CreatedAt,
                r.CreatedBy,
                r.PublishedAt,
                r.PublishedBy,
                r.Content.Length))
            .ToList();

        return TypedResults.Ok(new TemplateHistoryResponse(summaries, totalCount, page, pageSize));
    }

    // -------------------------------------------------------------------------
    // GET /{name}/history/{revisionId} — Full detail of a specific revision
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplateRevisionResponse>, NotFound, ProblemHttpResult>> HandleGetRevisionDetailAsync(
        HttpContext context,
        string name,
        Guid revisionId,
        string? culture,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        TemplateKey key = new(name, culture);
        IReadOnlyList<TemplateRevision> history =
            await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);

        TemplateRevision? revision = history.FirstOrDefault(r => r.RevisionId == revisionId);
        if (revision is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToRevisionResponse(revision));
    }

    // -------------------------------------------------------------------------
    // POST /{name}/preview — Render the current draft with test data
    // -------------------------------------------------------------------------

    private static async Task<Results<Ok<TemplatePreviewResponse>, NotFound, ProblemHttpResult>> HandlePreviewAsync(
        HttpContext context,
        string name,
        TemplatePreviewRequest body,
        CancellationToken ct)
    {
        IDocumentTemplateStoreReader? storeReader =
            context.RequestServices.GetService<IDocumentTemplateStoreReader>();

        if (storeReader is null)
        {
            return StoreNotRegistered();
        }

        ProblemHttpResult? nameError = ValidateTemplateName(name);
        if (nameError is not null)
        {
            return nameError;
        }

        if (body.Culture is not null)
        {
            ProblemHttpResult? cultureError = ValidateBcp47(body.Culture);
            if (cultureError is not null)
            {
                return cultureError;
            }
        }

        TemplateKey key = new(name, body.Culture);
        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);

        if (draft is null)
        {
            return TypedResults.NotFound();
        }

        List<ITemplateEngine> engines =
            context.RequestServices.GetServices<ITemplateEngine>().ToList();

        if (engines.Count == 0)
        {
            return TypedResults.Problem(
                detail: "No template engine is registered. Add Granit.Templating.Scriban to enable rendering.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        TemplateDescriptor descriptor = new()
        {
            Content = draft.Content,
            MimeType = draft.MimeType,
            RevisionId = draft.RevisionId,
        };

        ITemplateEngine? engine = engines.FirstOrDefault(e => e.CanRender(descriptor));
        if (engine is null)
        {
            return TypedResults.Problem(
                detail: $"No template engine can render MIME type '{draft.MimeType}'.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        IEnumerable<ITemplateGlobalContext> globalContexts =
            context.RequestServices.GetServices<ITemplateGlobalContext>();

        Dictionary<string, object?> data = body.Data.HasValue
            ? ConvertJsonObject(body.Data.Value)
            : [];

        var sw = Stopwatch.StartNew();

        RenderedContent rendered;
        try
        {
            rendered = await engine.RenderAsync(
                descriptor,
                data,
                DocumentFormat.Html,
                globalContexts.ToList(),
                ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return TypedResults.Problem(
                detail: $"Template rendering failed: {ex.Message}",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        sw.Stop();

        if (rendered is not TextRenderedContent textContent)
        {
            return TypedResults.Problem(
                detail: "Preview is only supported for text-based templates (HTML). Binary templates (Excel) cannot be previewed.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return TypedResults.Ok(new TemplatePreviewResponse(
            textContent.Html,
            rendered.RevisionId,
            sw.ElapsedMilliseconds));
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private static string GetCurrentUserId(HttpContext context)
    {
        ICurrentUserService? userService = context.RequestServices.GetService<ICurrentUserService>();
        return userService?.UserId ?? userService?.UserName ?? "unknown";
    }

    private static TemplateRevisionResponse ToRevisionResponse(TemplateRevision revision) =>
        new(
            revision.RevisionId,
            revision.Content,
            revision.MimeType,
            revision.Status,
            revision.CreatedAt,
            revision.CreatedBy,
            revision.PublishedAt,
            revision.PublishedBy);

    private static async Task<TemplateDetailResponse> BuildDetailResponseAsync(
        IDocumentTemplateStoreReader storeReader,
        TemplateKey key,
        CancellationToken ct)
    {
        TemplateRevision? draft = await storeReader.TryGetDraftAsync(key, ct).ConfigureAwait(false);
        Pipeline.TemplateDescriptor? published = await storeReader.TryGetPublishedAsync(key, ct).ConfigureAwait(false);

        TemplateRevisionResponse? publishedResponse = null;
        if (published is not null)
        {
            IReadOnlyList<TemplateRevision> history = await storeReader.GetHistoryAsync(key, ct).ConfigureAwait(false);
            TemplateRevision? publishedRevision = history.FirstOrDefault(
                r => r.Status == TemplateLifecycleStatus.Published);

            if (publishedRevision is not null)
            {
                publishedResponse = ToRevisionResponse(publishedRevision);
            }
        }

        return new TemplateDetailResponse(
            key.Name,
            key.Culture,
            draft is not null ? ToRevisionResponse(draft) : null,
            publishedResponse);
    }

    private static ProblemHttpResult StoreNotRegistered() =>
        TypedResults.Problem(
            detail: "No template store is registered. Add Granit.Templating.EntityFrameworkCore to enable persistence.",
            statusCode: StatusCodes.Status501NotImplemented);

    private static ProblemHttpResult? ValidateTemplateName(string name)
    {
        if (name.Length > MaxNameLength)
        {
            return TypedResults.Problem(
                detail: $"Template name must not exceed {MaxNameLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TemplateNamePattern().IsMatch(name))
        {
            return TypedResults.Problem(
                detail: "Template name must follow the 'Domain.Name' pattern (e.g. 'Billing.Invoice').",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private static ProblemHttpResult? ValidateBcp47(string cultureName) =>
        Bcp47Pattern().IsMatch(cultureName)
            ? null
            : TypedResults.Problem(
                detail: $"Culture name '{cultureName}' is not a valid BCP 47 tag.",
                statusCode: StatusCodes.Status400BadRequest);

    private static ProblemHttpResult? ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            return TypedResults.Problem(
                detail: "Page must be at least 1.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (pageSize is < 1 or > 100)
        {
            return TypedResults.Problem(
                detail: "PageSize must be between 1 and 100.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> object to a <see cref="Dictionary{TKey, TValue}"/>
    /// suitable for Scriban template rendering.
    /// </summary>
    private static Dictionary<string, object?> ConvertJsonObject(JsonElement element)
    {
        Dictionary<string, object?> dict = [];

        if (element.ValueKind != JsonValueKind.Object)
        {
            return dict;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonValue(property.Value);
        }

        return dict;
    }

    private static object? ConvertJsonValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
}
