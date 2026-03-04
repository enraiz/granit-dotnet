using FluentValidation;
using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.Validation;

namespace Granit.DataExchange.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="SaveExportPresetRequest"/> body for export preset creation.
/// </summary>
/// <remarks>
/// Validates structural constraints only. Business validation (definition existence)
/// is handled by the endpoint handler.
/// </remarks>
internal sealed class SaveExportPresetRequestValidator : GranitValidator<SaveExportPresetRequest>
{
    public SaveExportPresetRequestValidator()
    {
        RuleFor(x => x.DefinitionName).NotEmpty();
        RuleFor(x => x.PresetName).NotEmpty();
        RuleFor(x => x.SelectedFields).NotEmpty();
        RuleFor(x => x.Format).NotEmpty();
    }
}
