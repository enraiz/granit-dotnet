using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// GR-MIGA001 — Reports an error when a <c>DropColumn</c> call inside an EF Core
/// <c>Migration</c> class is not annotated with <c>[MigrationCycle(MigrationPhase.Contract, ...)]</c>.
/// </summary>
/// <remarks>
/// Opt-in: only activates when <c>Granit.Persistence.Migrations</c> is referenced in the
/// compiled project (i.e., when <c>MigrationCycleAttribute</c> is found in the compilation).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DropColumnWithoutContractAnalyzer : GranitMigrationAnalyzerBase
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA001";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "DropColumn requires a Contract-phase annotation",
        messageFormat: "Migration '{0}' calls DropColumn but is not annotated with [MigrationCycle(MigrationPhase.Contract, ...)]",
        category: "Migrations",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "DropColumn is only safe in the Contract phase of the Expand & Contract pattern. "
            + "Annotate the migration class with [MigrationCycle(MigrationPhase.Contract, \"cycle-id\")].");

    /// <inheritdoc/>
    protected override DiagnosticDescriptor Rule => _rule;

    /// <inheritdoc/>
    protected override string TargetMethodName => "DropColumn";

    /// <inheritdoc/>
    protected override void AnalyzeMigrationInvocation(
        SyntaxNodeAnalysisContext context,
        (INamedTypeSymbol MigrationClass, IMethodSymbol Method) migration,
        INamedTypeSymbol cycleAttrType)
    {
        if (MigrationAnalyzerHelpers.HasContractAnnotation(migration.MigrationClass, cycleAttrType))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(_rule, context.Node.GetLocation(), migration.MigrationClass.Name));
    }
}
