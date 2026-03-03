using System.Runtime.CompilerServices;
using System.Text;
using Granit.DataExchange.Import.Parsing;
using nietras.SeparatedValues;

namespace Granit.DataExchange.Csv.Internal.Import;

/// <summary>
/// CSV file parser backed by <see href="https://github.com/nietras/Sep">Sep</see>.
/// Supports streaming via <see cref="IAsyncEnumerable{T}"/> — only one row is in memory at a time.
/// </summary>
internal sealed class SepCsvFileParser : IFileParser
{
    private static readonly string[] SupportedMimeTypes =
        ["text/csv", "application/csv"];

    /// <inheritdoc/>
    public bool CanParse(string mimeType) =>
        SupportedMimeTypes.Any(m => string.Equals(m, mimeType, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ExtractHeadersAsync(
        Stream stream,
        FileParsingOptions options,
        CancellationToken ct = default)
    {
        using SepReader reader = CreateReader(stream, options);
        IReadOnlyList<string> headers = reader.Header.ColNames.ToList().AsReadOnly();
        return Task.FromResult(headers);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string[]>> ReadPreviewAsync(
        Stream stream,
        FileParsingOptions options,
        int maxRows = 10,
        CancellationToken ct = default)
    {
        using SepReader reader = CreateReader(stream, options);
        IReadOnlyList<string> colNames = reader.Header.ColNames;

        List<string[]> rows = [];
        int count = 0;

        foreach (SepReader.Row row in reader)
        {
            if (count >= maxRows)
            {
                break;
            }

            string[] values = new string[colNames.Count];
            for (int i = 0; i < colNames.Count; i++)
            {
                values[i] = row[i].ToString();
            }

            rows.Add(values);
            count++;
        }

        IReadOnlyList<string[]> result = rows.AsReadOnly();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<RawImportRow> ParseAsync(
        Stream stream,
        FileParsingOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using SepReader reader = CreateReader(stream, options);
        IReadOnlyList<string> colNames = reader.Header.ColNames;
        int rowNumber = 0;

        foreach (SepReader.Row row in reader)
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            Dictionary<string, string?> values = new(colNames.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < colNames.Count; i++)
            {
                string value = row[i].ToString();
                values[colNames[i]] = value.Length == 0 ? null : value;
            }

            yield return new RawImportRow(rowNumber, values);
        }

        await Task.CompletedTask;
    }

    private static SepReader CreateReader(Stream stream, FileParsingOptions options)
    {
        char separator = options.Separator.Length > 0 ? options.Separator[0] : ',';
        SepReaderOptions readerOptions = Sep.New(separator).Reader(o => o with { HasHeader = true, Unescape = true });

        if (options.Encoding is not null)
        {
            var encoding = Encoding.GetEncoding(options.Encoding);
            StreamReader textReader = new(stream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            return readerOptions.From(textReader);
        }

        return readerOptions.From(stream);
    }
}
