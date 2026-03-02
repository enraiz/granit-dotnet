using Granit.DataImport.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.DataImport.Internal;

/// <summary>
/// Default implementation of the 4-tier mapping suggestion pipeline:
/// Saved → Exact → Fuzzy → Semantic (AI).
/// </summary>
/// <remarks>
/// Columns matched by a higher-confidence tier are excluded from lower tiers.
/// Deduplication keeps the best (lowest enum value) confidence per source column.
/// </remarks>
internal sealed class MappingSuggestionService(
    IServiceProvider serviceProvider,
    ISemanticMappingService semanticMappingService,
    IOptions<DataImportOptions> options) : IMappingSuggestionService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ColumnMapping>> SuggestMappingsAsync<TEntity>(
        IReadOnlyList<string> headers,
        CancellationToken ct = default) where TEntity : class
    {
        ImportDefinition<TEntity> definition = serviceProvider.GetRequiredService<ImportDefinition<TEntity>>();
        IReadOnlyList<PropertyMapping> properties = definition.GetProperties();

        Dictionary<string, ColumnMapping> suggestions = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> matchedTargets = new(StringComparer.OrdinalIgnoreCase);

        // Tier 1: Saved mappings
        IMappingStore? mappingStore = serviceProvider.GetService<IMappingStore>();
        if (mappingStore is not null)
        {
            IReadOnlyList<ColumnMapping> saved = await mappingStore.LoadAsync(definition.Name, ct).ConfigureAwait(false);
            ApplySuggestions(suggestions, matchedTargets, headers, saved);
        }

        // Tier 2: Exact match (property name, display name, or aliases — case-insensitive)
        ApplyExactMatches(suggestions, matchedTargets, headers, properties);

        // Tier 3: Fuzzy match (Levenshtein)
        ApplyFuzzyMatches(suggestions, matchedTargets, headers, properties, options.Value.FuzzyMatchThreshold);

        // Tier 4: Semantic (AI) — only if available and unmapped columns remain
        List<string> unmappedHeaders = headers
            .Where(h => !suggestions.ContainsKey(h))
            .ToList();

        if (semanticMappingService.IsAvailable && unmappedHeaders.Count > 0)
        {
            IReadOnlyList<FieldMetadata> targetFields = definition.GetFieldMetadata();
            IReadOnlyList<SemanticMappingSuggestion> semanticSuggestions =
                await semanticMappingService.SuggestSemanticMappingsAsync(unmappedHeaders, targetFields, ct).ConfigureAwait(false);

            foreach (SemanticMappingSuggestion suggestion in semanticSuggestions
                .Where(s => !suggestions.ContainsKey(s.SourceColumn) &&
                             !matchedTargets.Contains(s.TargetProperty)))
            {
                suggestions[suggestion.SourceColumn] = new ColumnMapping(
                    suggestion.SourceColumn, suggestion.TargetProperty, MappingConfidence.Semantic);
                matchedTargets.Add(suggestion.TargetProperty);
            }
        }

        return suggestions.Values.ToList().AsReadOnly();
    }

    private static void ApplySuggestions(
        Dictionary<string, ColumnMapping> suggestions,
        HashSet<string> matchedTargets,
        IReadOnlyList<string> headers,
        IReadOnlyList<ColumnMapping> saved)
    {
        foreach (ColumnMapping mapping in saved
            .Where(m => m.TargetProperty is not null &&
                        headers.Contains(m.SourceColumn, StringComparer.OrdinalIgnoreCase) &&
                        !matchedTargets.Contains(m.TargetProperty)))
        {
            suggestions[mapping.SourceColumn] = mapping with { Confidence = MappingConfidence.Saved };
            matchedTargets.Add(mapping.TargetProperty!);
        }
    }

    private static void ApplyExactMatches(
        Dictionary<string, ColumnMapping> suggestions,
        HashSet<string> matchedTargets,
        IReadOnlyList<string> headers,
        IReadOnlyList<PropertyMapping> properties)
    {
        foreach (string header in headers)
        {
            if (suggestions.ContainsKey(header))
            {
                continue;
            }

            PropertyMapping? match = properties.FirstOrDefault(p =>
                !matchedTargets.Contains(p.PropertyPath) &&
                (string.Equals(header, p.PropertyPath, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(header, p.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                 p.Aliases.Any(a => string.Equals(header, a, StringComparison.OrdinalIgnoreCase))));

            if (match is not null)
            {
                suggestions[header] = new ColumnMapping(header, match.PropertyPath, MappingConfidence.Exact);
                matchedTargets.Add(match.PropertyPath);
            }
        }
    }

    private static void ApplyFuzzyMatches(
        Dictionary<string, ColumnMapping> suggestions,
        HashSet<string> matchedTargets,
        IReadOnlyList<string> headers,
        IReadOnlyList<PropertyMapping> properties,
        double threshold)
    {
        foreach (string header in headers)
        {
            if (suggestions.ContainsKey(header))
            {
                continue;
            }

            (PropertyMapping? bestMatch, double bestScore) = FindBestFuzzyMatch(
                header, properties, matchedTargets);

            if (bestMatch is not null && bestScore >= threshold)
            {
                suggestions[header] = new ColumnMapping(header, bestMatch.PropertyPath, MappingConfidence.Fuzzy);
                matchedTargets.Add(bestMatch.PropertyPath);
            }
        }
    }

    private static (PropertyMapping? Match, double Score) FindBestFuzzyMatch(
        string header,
        IReadOnlyList<PropertyMapping> properties,
        HashSet<string> matchedTargets)
    {
        PropertyMapping? bestMatch = null;
        double bestScore = 0;

        foreach (PropertyMapping property in properties)
        {
            if (matchedTargets.Contains(property.PropertyPath))
            {
                continue;
            }

            double score = ComputeNormalizedLevenshteinSimilarity(
                header.ToUpperInvariant(),
                (property.DisplayName ?? property.PropertyPath).ToUpperInvariant());

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = property;
            }

            foreach (string alias in property.Aliases)
            {
                double aliasScore = ComputeNormalizedLevenshteinSimilarity(
                    header.ToUpperInvariant(), alias.ToUpperInvariant());
                if (aliasScore > bestScore)
                {
                    bestScore = aliasScore;
                    bestMatch = property;
                }
            }
        }

        return (bestMatch, bestScore);
    }

    /// <summary>
    /// Computes the normalized Levenshtein similarity (1.0 = identical, 0.0 = completely different).
    /// </summary>
    internal static double ComputeNormalizedLevenshteinSimilarity(string source, string target)
    {
        if (source.Length == 0 && target.Length == 0)
        {
            return 1.0;
        }

        int maxLen = Math.Max(source.Length, target.Length);
        int distance = ComputeLevenshteinDistance(source, target);
        return 1.0 - ((double)distance / maxLen);
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        int sourceLength = source.Length;
        int targetLength = target.Length;

        if (sourceLength == 0)
        {
            return targetLength;
        }

        if (targetLength == 0)
        {
            return sourceLength;
        }

        int[] previousRow = new int[targetLength + 1];
        int[] currentRow = new int[targetLength + 1];

        for (int j = 0; j <= targetLength; j++)
        {
            previousRow[j] = j;
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                currentRow[j] = Math.Min(
                    Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1),
                    previousRow[j - 1] + cost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }
}
