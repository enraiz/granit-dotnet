using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using WolverineOpenApiSpike;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// --- Authentication stub (no real JWT, just enough for [Authorize] metadata) ---
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, NoOpAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

// --- OpenAPI: single document "v1" ---
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Wolverine OpenAPI Spike",
            Version = "1.0.0",
            Description = "Spike to validate Wolverine HTTP + Microsoft.AspNetCore.OpenApi integration"
        };
        return Task.CompletedTask;
    });

    // Q2 — Transformer that inspects endpoint metadata
    options.AddOperationTransformer((operation, context, ct) =>
    {
        IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;

        // Log all metadata types for diagnostics
        Console.WriteLine($"[TRANSFORMER] {context.Description.HttpMethod} {context.Description.RelativePath}");
        foreach (object m in metadata)
        {
            Console.WriteLine($"  metadata: {m.GetType().FullName} → {m}");
        }

        // Check for custom attribute
        bool hasAllowAnonymousTenant = metadata
            .OfType<AllowAnonymousTenantAttribute>().Any();

        if (!hasAllowAnonymousTenant)
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Tenant identifier"
            });
        }

        // Check for [Authorize]
        bool hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();
        bool hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= [];
            OpenApiSecuritySchemeReference schemeRef = new("Bearer", context.Document);
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [schemeRef] = new List<string>()
            });
        }

        return Task.CompletedTask;
    });
});

// --- OpenAPI: second document "v1-internal" for Q4 ---
builder.Services.AddOpenApi("v1-internal", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Wolverine OpenAPI Spike — Internal",
            Version = "1.0.0-internal"
        };
        return Task.CompletedTask;
    });
});

// --- Wolverine ---
builder.Services.AddWolverineHttp();
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

WebApplication app = builder.Build();

// --- Middleware ---
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

// --- One minimal API endpoint for comparison ---
app.MapGet("/minimal/ping", () => Results.Ok(new { Message = "pong" }))
    .WithName("MinimalPing")
    .WithGroupName("v1")
    .WithTags("Minimal");

app.MapWolverineEndpoints();

app.Run();
