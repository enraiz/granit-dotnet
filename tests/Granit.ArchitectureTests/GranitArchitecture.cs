using System.Reflection;
using ArchUnitNET.Loader;

namespace Granit.ArchitectureTests;

/// <summary>
/// Loads the entire Granit architecture graph once per test run.
/// All test classes share this static instance to avoid repeated assembly scanning.
/// </summary>
internal static class GranitArchitecture
{
    internal static readonly ArchUnitNET.Domain.Architecture Instance = new ArchLoader()
        .LoadAssemblies(GetGranitAssemblies())
        .Build();

    private static Assembly[] GetGranitAssemblies()
    {
        // Scan the output directory for all Granit.*.dll files.
        // ProjectReferences ensure they are copied here at build time.
        string outputDir = Path.GetDirectoryName(typeof(GranitArchitecture).Assembly.Location)!;

        return Directory.GetFiles(outputDir, "Granit.*.dll")
            .Where(path =>
            {
                string name = Path.GetFileNameWithoutExtension(path);
                return !name.Contains("Tests", StringComparison.Ordinal)
                    && !name.Contains("Analyzers", StringComparison.Ordinal)
                    && !name.Contains("SourceGenerator", StringComparison.Ordinal);
            })
            .Select(path =>
            {
                try
                {
                    return Assembly.LoadFrom(path);
                }
                catch
                {
                    return null;
                }
            })
            .Where(a => a is not null)
            .ToArray()!;
    }
}
