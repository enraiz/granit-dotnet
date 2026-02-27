using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// GR-MIGA003 — Reports a warning when <c>AddColumn</c> is called with <c>nullable: false</c>
/// (or without an explicit <c>nullable</c> argument) and without a <c>defaultValue</c> or
/// <c>defaultValueSql</c> argument.
/// </summary>
/// <remarks>
/// Adding a NOT NULL column without a default value requires a full table rewrite on most
/// databases (table lock), making it unsafe for zero-downtime deployments.
/// Safe alternatives:
/// <list type="bullet">
///   <item>Add the column as nullable (<c>nullable: true</c>) in the Expand phase.</item>
///   <item>Provide a <c>defaultValue</c> or <c>defaultValueSql</c> so existing rows are filled.</item>
/// </list>
/// Opt-in: only activates when <c>Granit.Persistence.Migrations</c> is referenced.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullableColumnInExpandAnalyzer : GranitMigrationAnalyzerBase
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA003";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "AddColumn NOT NULL without a default value risks a table lock",
        messageFormat: "Migration '{0}' adds a NOT NULL column without a defaultValue or defaultValueSql. "
            + "Use nullable: true (Expand phase) or provide a default value.",
        category: "Migrations",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Adding a NOT NULL column without a default value causes a full table rewrite "
            + "on most databases (table lock). Add the column as nullable in the Expand phase, "
            + "backfill data, then add the NOT NULL constraint in the Contract phase.");

    /// <inheritdoc/>
    protected override DiagnosticDescriptor Rule => _rule;

    /// <inheritdoc/>
    protected override void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase,
        INamedTypeSymbol cycleAttrType)
    {
        (INamedTypeSymbol MigrationClass, IMethodSymbol Method)? result =
            MigrationAnalyzerHelpers.TryGetMigrationInvocation(context, migrationBase, "AddColumn");
        if (result is null)
        {
            return;
        }

        ArgumentListSyntax argList = ((InvocationExpressionSyntax)context.Node).ArgumentList;

        // Safe: nullable: true
        if (MigrationAnalyzerHelpers.HasNamedArgumentWithTrueValue(argList, "nullable", context.SemanticModel))
        {
            return;
        }

        // Safe: defaultValue or defaultValueSql is provided
        if (MigrationAnalyzerHelpers.HasNamedArgument(argList, "defaultValue")
            || MigrationAnalyzerHelpers.HasNamedArgument(argList, "defaultValueSql"))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(_rule, context.Node.GetLocation(), result.Value.MigrationClass.Name));
    }
}
