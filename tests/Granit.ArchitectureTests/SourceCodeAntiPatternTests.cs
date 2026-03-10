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
}
