using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Granit.ApiDocumentation.Transformers;

/// <summary>
/// Normalizes nullable integer query parameters that ASP.NET Core generates as
/// <c>type: ["integer", "string"]</c> back to <c>type: "integer"</c>.
/// This is an artifact of model binding from query strings where <c>int?</c> parameters
/// accept both integer and string representations.
/// </summary>
internal sealed class NullableIntSchemaOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Parameters is null)
        {
            return Task.CompletedTask;
        }

        foreach (OpenApiParameter parameter in operation.Parameters)
        {
            if (parameter.Schema is not OpenApiSchema schema)
            {
                continue;
            }

            if (schema.Type == (JsonSchemaType.Integer | JsonSchemaType.String)
                && schema.Format is "int32" or "int64")
            {
                schema.Type = JsonSchemaType.Integer | JsonSchemaType.Null;
            }
        }

        return Task.CompletedTask;
    }
}
