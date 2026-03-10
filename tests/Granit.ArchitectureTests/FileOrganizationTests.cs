using Shouldly;
using Xunit;

namespace Granit.ArchitectureTests;

/// <summary>
/// Enforces file organization conventions: types with well-known suffixes
/// must reside in the corresponding subfolder within their module.
/// Uses filesystem scanning to correlate file names with directory structure.
/// </summary>
public sealed class FileOrganizationTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string SrcRoot = Path.Combine(RepoRoot, "src");

    /// <summary>
    /// Module classes (Granit*Module.cs) must be at the module root, not nested.
    /// The only exception is the abstract base class GranitModule in Granit.Core/Modularity/.
    /// </summary>
    [Fact]
    public void Module_classes_should_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Module.cs", StringComparison.Ordinal)
                || !fileName.StartsWith("Granit", StringComparison.Ordinal))
            {
                continue;
            }

            // GranitModule.cs base class is in Granit.Core/Modularity/ by design
            if (fileName == "GranitModule.cs")
            {
                continue;
            }

            if (!IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Module classes (Granit*Module.cs) must be at the module root directory, not in subfolders. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Extension classes (*Extensions.cs) should be in an Extensions/ subfolder,
    /// not at the module root. Files in other purposeful subfolders (Internal/, Domain/, etc.) are allowed.
    /// </summary>
    [Fact]
    public void Extension_classes_should_not_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Extensions.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Extension classes should be in an Extensions/ subfolder, not at the module root. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Exception classes (*Exception.cs) must be in an Exceptions/ subfolder.
    /// </summary>
    [Fact]
    public void Exception_classes_should_reside_in_Exceptions_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Exception.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Exceptions"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Exception classes must reside in an Exceptions/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Options classes (*Options.cs) should not be at the module root.
    /// They must be in an Options/, Internal/, or other purposeful subfolder.
    /// </summary>
    [Fact]
    public void Options_classes_should_not_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Options.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Options classes should be in an Options/ or Internal/ subfolder, not at the module root. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// In *.Endpoints modules, endpoint classes (*Endpoints.cs) must be in an Endpoints/
    /// or Internal/ subfolder.
    /// </summary>
    [Fact]
    public void Endpoint_classes_should_reside_in_Endpoints_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Endpoints.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleName = GetModuleName(csFile);
            if (!moduleName.EndsWith(".Endpoints", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Endpoints") && !IsInFolder(csFile, "Internal"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Endpoint classes in *.Endpoints modules must reside in an Endpoints/ or Internal/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// In *.Endpoints modules, DTO classes (*Request.cs, *Response.cs) must be in a Dtos/ subfolder.
    /// </summary>
    [Fact]
    public void Dto_classes_should_reside_in_Dtos_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Request.cs", StringComparison.Ordinal)
                && !fileName.EndsWith("Response.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleName = GetModuleName(csFile);
            if (!moduleName.EndsWith(".Endpoints", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Dtos"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "DTO classes (*Request.cs, *Response.cs) in *.Endpoints modules must reside in a Dtos/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// In *.EntityFrameworkCore modules, entity configuration classes (*Configuration.cs) must
    /// be in a Configurations/, EntityConfigurations/, or Internal/ subfolder.
    /// </summary>
    [Fact]
    public void EfCore_configurations_should_reside_in_Configurations_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Configuration.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleName = GetModuleName(csFile);
            if (!moduleName.Contains("EntityFrameworkCore", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Configurations")
                && !IsInFolder(csFile, "EntityConfigurations")
                && !IsInFolder(csFile, "Internal"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "EF Core configuration classes must reside in Configurations/, EntityConfigurations/, " +
            "or Internal/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Health check classes (*HealthCheck.cs) must not be at the module root.
    /// They should reside in HealthChecks/, Internal/, or another purposeful subfolder.
    /// </summary>
    [Fact]
    public void HealthCheck_classes_should_not_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("HealthCheck.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Health check classes should be in a HealthChecks/ or Internal/ subfolder, not at the module root. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Interceptor classes (*Interceptor.cs) must not be at the module root.
    /// They should reside in Interceptors/, Internal/, or another purposeful subfolder.
    /// </summary>
    [Fact]
    public void Interceptor_classes_should_not_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Interceptor.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Interceptor classes should be in an Interceptors/ or Internal/ subfolder, not at the module root. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// In *.Endpoints modules, validator classes (*Validator.cs) must be in a Validators/
    /// or Internal/ subfolder.
    /// </summary>
    [Fact]
    public void Endpoint_validators_should_reside_in_Validators_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Validator.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleName = GetModuleName(csFile);
            if (!moduleName.EndsWith(".Endpoints", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Validators") && !IsInFolder(csFile, "Internal"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Validator classes in *.Endpoints modules must reside in a Validators/ or Internal/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// In *.Endpoints modules, permission definition classes (*Permissions.cs,
    /// *PermissionDefinitionProvider.cs) must be in a Permissions/ subfolder.
    /// Files with "Permission" in the name that have other suffixes (Endpoints, Response, Request)
    /// are exempt — they belong in their respective folders (Endpoints/, Dtos/).
    /// </summary>
    [Fact]
    public void Permission_definitions_should_reside_in_Permissions_folder()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);

            // Only check permission definition files (static constants classes and providers)
            if (!fileName.EndsWith("Permissions.cs", StringComparison.Ordinal)
                && !fileName.EndsWith("PermissionDefinitionProvider.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleName = GetModuleName(csFile);
            if (!moduleName.EndsWith(".Endpoints", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsInFolder(csFile, "Permissions"))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Permission definition classes in *.Endpoints modules must reside in a Permissions/ subfolder. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Store classes (*Store.cs) should not be at the module root.
    /// They should reside in Internal/, Stores/, or another purposeful subfolder.
    /// </summary>
    [Fact]
    public void Store_classes_should_not_be_at_module_root()
    {
        List<string> violations = [];

        foreach (string csFile in GetSrcCsFiles())
        {
            string fileName = Path.GetFileName(csFile);
            if (!fileName.EndsWith("Store.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // Interfaces (I*Store.cs) are contracts and may live at module root
            if (fileName.StartsWith('I') && char.IsUpper(fileName[1]))
            {
                continue;
            }

            if (IsAtModuleRoot(csFile))
            {
                violations.Add(Path.GetRelativePath(SrcRoot, csFile));
            }
        }

        violations.ShouldBeEmpty(
            "Store implementation classes should be in an Internal/, Stores/, or other subfolder, " +
            "not at the module root. " +
            $"Violators: {string.Join(", ", violations)}");
    }

    private static IEnumerable<string> GetSrcCsFiles() =>
        Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

    /// <summary>
    /// Checks if a file is directly at its module root (not in any subfolder).
    /// </summary>
    private static bool IsAtModuleRoot(string filePath)
    {
        string relativePath = Path.GetRelativePath(SrcRoot, filePath);
        string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
        return parts.Length == 2;
    }

    /// <summary>
    /// Checks if a file resides in a folder with the given name (at any depth within its module).
    /// </summary>
    private static bool IsInFolder(string filePath, string folderName)
    {
        string sep = Path.DirectorySeparatorChar.ToString();
        return filePath.Contains(sep + folderName + sep, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the module name (first path component under src/) for a file.
    /// </summary>
    private static string GetModuleName(string filePath)
    {
        string relativePath = Path.GetRelativePath(SrcRoot, filePath);
        int sepIndex = relativePath.IndexOf(Path.DirectorySeparatorChar);
        return sepIndex >= 0 ? relativePath[..sepIndex] : relativePath;
    }

    private static string FindRepoRoot()
    {
        string? dir = Path.GetDirectoryName(typeof(FileOrganizationTests).Assembly.Location);
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
