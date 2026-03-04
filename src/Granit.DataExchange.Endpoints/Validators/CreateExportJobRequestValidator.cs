using FluentValidation;
using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.Validation;

namespace Granit.DataExchange.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="CreateExportJobRequest"/> body for export job creation.
/// </summary>
/// <remarks>
/// Validates structural constraints only. Business validation (definition existence,
/// supported formats) is handled by the endpoint handler.
/// </remarks>
internal sealed class CreateExportJobRequestValidator : GranitValidator<CreateExportJobRequest>
{
    public CreateExportJobRequestValidator()
    {
        RuleFor(x => x.DefinitionName).NotEmpty();
        RuleFor(x => x.Format).NotEmpty();
    }
}
