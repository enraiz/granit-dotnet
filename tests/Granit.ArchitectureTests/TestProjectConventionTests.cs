using Shouldly;
using Xunit;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates test project conventions: every src package must have a matching test project.
/// </summary>
public sealed class TestProjectConventionTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Every_src_package_should_have_a_test_project()
    {
        string srcDir = Path.Combine(RepoRoot, "src");
        string testsDir = Path.Combine(RepoRoot, "tests");

        // Packages excluded: Analyzers and SourceGenerator target netstandard2.0
        HashSet<string> excluded =
        [
            "Granit.Analyzers",
            "Granit.Analyzers.CodeFixes",
            "Granit.Localization.SourceGenerator",
        ];

        IEnumerable<string> srcPackages = Directory.GetDirectories(srcDir)
            .Select(Path.GetFileName)
            .Where(name => name!.StartsWith("Granit.", StringComparison.Ordinal))
            .Where(name => !excluded.Contains(name!))
            .Cast<string>();

        List<string> missing = [];
        foreach (string package in srcPackages)
        {
            bool hasUnitTests = Directory.Exists(Path.Combine(testsDir, $"{package}.Tests"));
            bool hasIntegrationTests = Directory.Exists(Path.Combine(testsDir, $"{package}.Tests.Integration"));

            if (!hasUnitTests && !hasIntegrationTests)
            {
                missing.Add(package);
            }
        }

        missing.ShouldBeEmpty(
            "Every src package must have a matching tests/ project (DoD). " +
            $"Missing: {string.Join(", ", missing)}");
    }

    private static string FindRepoRoot()
    {
        string? dir = Path.GetDirectoryName(typeof(TestProjectConventionTests).Assembly.Location);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find repository root (.git directory)");
    }
}
