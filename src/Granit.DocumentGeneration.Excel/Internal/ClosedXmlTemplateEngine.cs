using System.Text.Json;
using ClosedXML.Excel;
using Granit.Templating.GlobalContext;
using Granit.Templating.Pipeline;

// 'DocumentFormat' is a root namespace from DocumentFormat.OpenXml (transitive ClosedXML dep.).
// Use a distinct alias to avoid CS0118/CS0576.
using TemplatingDocFormat = Granit.Templating.Keys.DocumentFormat;

namespace Granit.DocumentGeneration.Excel.Internal;

/// <summary>
/// <see cref="ITemplateEngine"/> implementation backed by
/// <a href="https://closedxml.io/">ClosedXML</a>.
/// Supports <c>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet</c> MIME type.
/// </summary>
/// <remarks>
/// The template content stored in <see cref="TemplateDescriptor.Content"/> must be a
/// Base64-encoded XLSX workbook. The engine decodes it, replaces <c>{{model.property}}</c>
/// placeholders in string cells, and returns a <see cref="BinaryRenderedContent"/> with
/// the completed workbook bytes.
/// <para>
/// Nested objects are flattened with dot notation and arrays with bracket notation:
/// <c>{{model.address.city}}</c>, <c>{{model.lines[0].amount}}</c>.
/// </para>
/// <para>
/// Unlike text engines (Scriban), <c>ClosedXmlTemplateEngine</c> returns binary output
/// directly — no <c>IDocumentRenderer</c> is invoked by <c>DocumentGenerator</c>.
/// </para>
/// </remarks>
internal sealed class ClosedXmlTemplateEngine : ITemplateEngine
{
    private const string ExcelMimeType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <inheritdoc/>
    public bool CanRender(TemplateDescriptor descriptor) =>
        string.Equals(descriptor.MimeType, ExcelMimeType, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<RenderedContent> RenderAsync<TData>(
        TemplateDescriptor descriptor,
        TData data,
        TemplatingDocFormat targetFormat,
        IReadOnlyList<ITemplateGlobalContext> globalContexts,
        CancellationToken ct = default) where TData : notnull
    {
        byte[] templateBytes = Convert.FromBase64String(descriptor.Content);
        Dictionary<string, string> substitutions = BuildSubstitutions(data);

        using MemoryStream outputStream = new();
        using (MemoryStream inputStream = new(templateBytes))
        using (XLWorkbook workbook = new(inputStream))
        {
            ApplySubstitutions(workbook, substitutions);
            workbook.SaveAs(outputStream);
        }

        RenderedContent result = new BinaryRenderedContent(
            outputStream.ToArray(), TemplatingDocFormat.Excel)
        {
            RevisionId = descriptor.RevisionId,
        };

        return Task.FromResult(result);
    }

    private static Dictionary<string, string> BuildSubstitutions<TData>(TData data)
        where TData : notnull
    {
        JsonElement element = JsonSerializer.SerializeToElement(data, SnakeCaseOptions);
        Dictionary<string, string> subs = [];
        FlattenElement(element, "model", subs);
        return subs;
    }

    private static void FlattenElement(
        JsonElement element, string prefix, Dictionary<string, string> subs)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    FlattenElement(property.Value, $"{prefix}.{property.Name}", subs);
                }

                break;

            case JsonValueKind.Array:
                int i = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    FlattenElement(item, $"{prefix}[{i++}]", subs);
                }

                break;

            default:
                subs[prefix] = element.ToString();
                break;
        }
    }

    private static void ApplySubstitutions(
        XLWorkbook workbook, Dictionary<string, string> substitutions)
    {
        foreach (IXLWorksheet worksheet in workbook.Worksheets)
        {
            foreach (IXLCell cell in worksheet.CellsUsed())
            {
                if (cell.DataType != XLDataType.Text)
                {
                    continue;
                }

                string value = cell.GetValue<string>();
                foreach (KeyValuePair<string, string> sub in substitutions)
                {
                    value = value.Replace(
                        $"{{{{{sub.Key}}}}}",
                        sub.Value,
                        StringComparison.Ordinal);
                }

                cell.SetValue(value);
            }
        }
    }
}
