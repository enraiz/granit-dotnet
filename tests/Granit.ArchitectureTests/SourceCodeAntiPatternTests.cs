using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace Granit.ArchitectureTests;

/// <summary>
/// Detects common C# anti-patterns by scanning source files:
/// - async void methods (unhandled exceptions crash the process)
/// - throw ex; (destroys stack trace — use throw; instead)
/// </summary>
public sealed partial class SourceCodeAntiPatternTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Async_void_methods_should_not_exist_in_src()
    {
        string srcDir = Path.Combine(RepoRoot, "src");

        List<string> violations = [];

        foreach (string csFile in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(csFile);

            foreach (Match match in AsyncVoidMethod().Matches(content))
            {
                string relativePath = Path.GetRelativePath(RepoRoot, csFile);
                int lineNumber = content[..match.Index].Count(c => c == '\n') + 1;
                violations.Add($"{relativePath}:{lineNumber}");
            }
        }

        violations.ShouldBeEmpty(
            "async void methods are forbidden — exceptions cannot be caught and will crash the process. " +
            "Use async Task instead. The only valid exception is event handlers, which do not exist in Granit. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Throw_ex_should_not_be_used_in_src()
    {
        string srcDir = Path.Combine(RepoRoot, "src");

        List<string> violations = [];

        foreach (string csFile in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(csFile);

            foreach (Match match in ThrowExStatement().Matches(content))
            {
                string relativePath = Path.GetRelativePath(RepoRoot, csFile);
                int lineNumber = content[..match.Index].Count(c => c == '\n') + 1;
                violations.Add($"{relativePath}:{lineNumber}");
            }
        }

        violations.ShouldBeEmpty(
            "throw ex; destroys the original stack trace — use throw; to preserve it. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Namespace declarations must match the file's location on disk.
    /// Expected: {ProjectName}.{SubFolder1}.{SubFolder2} (dots replace path separators).
    /// Files at the module root should have namespace = project name.
    /// </summary>
    [Fact]
    public void Namespace_should_match_folder_structure_in_src()
    {
        string srcDir = Path.Combine(RepoRoot, "src");

        List<string> violations = [];

        foreach (string csFile in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            if (csFile.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                || csFile.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            string content = File.ReadAllText(csFile);

            Match nsMatch = NamespaceDeclaration().Match(content);
            if (!nsMatch.Success)
            {
                continue;
            }

            string actualNamespace = nsMatch.Groups[1].Value;
            string expectedNamespace = ComputeExpectedNamespace(srcDir, csFile);

            if (!string.Equals(actualNamespace, expectedNamespace, StringComparison.Ordinal))
            {
                string relativePath = Path.GetRelativePath(RepoRoot, csFile);
                violations.Add($"{relativePath} (expected: {expectedNamespace}, actual: {actualNamespace})");
            }
        }

        violations.ShouldBeEmpty(
            "Namespace must match the folder structure: {ProjectName}.{SubFolder1}.{SubFolder2}. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Computes the expected namespace from a file's disk path.
    /// </summary>
    private static string ComputeExpectedNamespace(string srcRoot, string filePath)
    {
        // relativePath = "Granit.Foo/Internal/Bar.cs"
        string relativePath = Path.GetRelativePath(srcRoot, filePath);
        string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

        // parts[0] = project name, parts[1..^1] = subfolders, parts[^1] = filename
        string projectName = parts[0];

        if (parts.Length <= 2)
        {
            // File at module root: namespace = project name
            return projectName;
        }

        // Join project name + subfolders (skip filename)
        string subfolders = string.Join('.', parts[1..^1]);
        return $"{projectName}.{subfolders}";
    }

    private static string FindRepoRoot()
    {
        string? dir = Path.GetDirectoryName(typeof(SourceCodeAntiPatternTests).Assembly.Location);
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

    /// <summary>
    /// Matches "async void MethodName" — excludes comments (lines starting with ///).
    /// Requires whitespace before async (indentation) to avoid matching inside string literals.
    /// </summary>
    [GeneratedRegex(@"(?<=^[ \t]+(?:(?:public|private|protected|internal|static|override|sealed|virtual|new)\s+)*)async\s+void\s+\w+", RegexOptions.Multiline)]
    private static partial Regex AsyncVoidMethod();

    /// <summary>
    /// Matches "throw identifier;" where identifier is NOT "new" (which would be throw new ...).
    /// This catches "throw ex;" and "throw exception;" patterns that destroy stack traces.
    /// </summary>
    [GeneratedRegex(@"\bthrow\s+(?!new\b)[a-zA-Z_]\w*\s*;")]
    private static partial Regex ThrowExStatement();

    /// <summary>
    /// Matches file-scoped namespace (<c>namespace X.Y.Z;</c>) or block-scoped
    /// (<c>namespace X.Y.Z {</c>). Captures the namespace identifier.
    /// </summary>
    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*[;{]", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();
}
