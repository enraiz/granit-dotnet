using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace WolverineOpenApiSpike.Endpoints;

/// <summary>
/// Q1 — Are Wolverine endpoints discovered by the OpenAPI document generator?
/// Q2 — Do [Authorize] and [Tags] metadata propagate to transformers?
/// Q3 — Are request/response types correctly translated to JSON schemas?
/// </summary>
public static class PatientEndpoints
{
    // GET with typed response — tests Q1 + Q3 (response schema)
    [WolverineGet("/api/patients")]
    [Tags("Patients")]
    public static PatientListItem[] GetAll()
    {
        return
        [
            new PatientListItem(Guid.NewGuid(), "Jean Dupont"),
            new PatientListItem(Guid.NewGuid(), "Marie Curie")
        ];
    }

    // GET with route parameter — tests Q3 (path param + response schema)
    [WolverineGet("/api/patients/{id}")]
    [Tags("Patients")]
    [Authorize]
    public static PatientResponse GetById(Guid id)
    {
        return new PatientResponse(id, "Jean", "Dupont", new DateOnly(1990, 1, 15));
    }

    // POST with command body — tests Q3 (request body schema + response schema)
    [WolverinePost("/api/patients")]
    [Tags("Patients")]
    [Authorize]
    public static PatientResponse Create(CreatePatientCommand command)
    {
        return new PatientResponse(
            Guid.NewGuid(),
            command.FirstName,
            command.LastName,
            command.BirthDate);
    }

    // DELETE — tests Q2 (Authorize metadata in transformer)
    [WolverineDelete("/api/patients/{id}")]
    [Tags("Patients")]
    [Authorize]
    public static void Delete(Guid id)
    {
        // no-op
    }
}
