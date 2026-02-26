using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace WolverineOpenApiSpike.Endpoints;

/// <summary>
/// Q4 — Internal-only endpoints to test multi-document filtering.
/// These should appear only in "v1-internal" document.
/// </summary>
public static class InternalEndpoints
{
    [WolverineGet("/internal/health/deep")]
    [Tags("Internal")]
    [Authorize]
    public static IResult DeepHealthCheck()
    {
        return Results.Ok(new { Status = "healthy", Database = true, Cache = true });
    }

    [WolverineGet("/internal/diagnostics/config")]
    [Tags("Internal")]
    [Authorize]
    public static IResult GetConfig()
    {
        return Results.Ok(new { Environment = "development", TenantIsolation = true });
    }
}
