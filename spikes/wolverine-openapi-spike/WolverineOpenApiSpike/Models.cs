namespace WolverineOpenApiSpike;

// --- Q3: Request/response types to validate schema generation ---

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string? SocialSecurityNumber);

public sealed record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly BirthDate);

public sealed record PatientListItem(
    Guid Id,
    string FullName);
