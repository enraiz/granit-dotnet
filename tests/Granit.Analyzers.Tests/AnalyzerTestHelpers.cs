using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers.Tests;

/// <summary>
/// Infrastructure for running Roslyn analyzers against in-memory C# compilations.
/// EF Core and Granit types are provided as source stubs — no real package references needed.
/// </summary>
internal static class AnalyzerTestHelpers
{
    /// <summary>
    /// Minimal EF Core stubs: <c>Migration</c> abstract class + <c>MigrationBuilder</c>
    /// with the methods targeted by GR-MIGA001–004.
    /// </summary>
    internal const string EfCoreMigrationsStub = """
        namespace Microsoft.EntityFrameworkCore.Migrations
        {
            public abstract class Migration
            {
                protected virtual void Up(MigrationBuilder migrationBuilder) { }
                protected virtual void Down(MigrationBuilder migrationBuilder) { }
            }

            public class MigrationBuilder
            {
                public void DropColumn(string name, string table = null, string schema = null) { }

                public void RenameColumn(string name, string table, string newName, string schema = null) { }

                public void AddColumn<T>(
                    string name,
                    string table = null,
                    string schema = null,
                    bool nullable = false,
                    object defaultValue = null,
                    string defaultValueSql = null) { }

                public void AlterColumn<T>(
                    string name,
                    string table = null,
                    string schema = null,
                    bool nullable = false,
                    System.Type oldClrType = null) { }
            }
        }
        """;

    /// <summary>
    /// Stub for <c>Granit.Persistence.Migrations.MigrationCycleAttribute</c> and
    /// <c>MigrationPhase</c> — simulates having <c>Granit.Persistence.Migrations</c> referenced.
    /// </summary>
    internal const string MigrationCycleAttributeStub = """
        namespace Granit.Persistence.Migrations
        {
            public enum MigrationPhase { Expand = 0, Migrate = 1, Contract = 2 }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class MigrationCycleAttribute : System.Attribute
            {
                public MigrationCycleAttribute(MigrationPhase phase, string cycleId) { }
            }
        }
        """;

    /// <summary>
    /// Runs <typeparamref name="TAnalyzer"/> against the given <paramref name="source"/> code,
    /// compiled together with the EF Core stub and optionally the <c>MigrationCycleAttribute</c> stub.
    /// </summary>
    /// <param name="source">The C# source snippet to analyze.</param>
    /// <param name="includeMigrationCycleAttribute">
    /// <see langword="true"/> (default) to simulate a project that references
    /// <c>Granit.Persistence.Migrations</c>; <see langword="false"/> to omit it.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    internal static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync<TAnalyzer>(
        string source,
        bool includeMigrationCycleAttribute = true,
        CancellationToken ct = default)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        SyntaxTree efCoreTree = CSharpSyntaxTree.ParseText(EfCoreMigrationsStub, cancellationToken: ct);

        ImmutableArray<SyntaxTree>.Builder treeBuilder = ImmutableArray.CreateBuilder<SyntaxTree>();
        treeBuilder.Add(sourceTree);
        treeBuilder.Add(efCoreTree);

        if (includeMigrationCycleAttribute)
        {
            treeBuilder.Add(CSharpSyntaxTree.ParseText(MigrationCycleAttributeStub, cancellationToken: ct));
        }

        ImmutableArray<MetadataReference> references = GetNetCoreReferences();

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: treeBuilder.ToImmutable(),
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(ct);
    }

    private static ImmutableArray<MetadataReference> GetNetCoreReferences()
    {
        string? assembliesStr = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (assembliesStr is null)
        {
            return ImmutableArray<MetadataReference>.Empty;
        }

        ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (string path in assembliesStr.Split(Path.PathSeparator))
        {
            builder.Add(MetadataReference.CreateFromFile(path));
        }

        return builder.ToImmutable();
    }
}
