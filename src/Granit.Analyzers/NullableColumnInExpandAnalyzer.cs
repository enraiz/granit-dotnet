using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
public sealed class NullableColumnInExpandAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA003";

    private static readonly DiagnosticDescriptor Rule = new(
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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Opt-in: only activate when Granit.Persistence.Migrations is referenced.
            INamedTypeSymbol? cycleAttrSymbol = compilationContext.Compilation
                .GetTypeByMetadataName("Granit.Persistence.Migrations.MigrationCycleAttribute");
            if (cycleAttrSymbol is null)
            {
                return;
            }

            INamedTypeSymbol? migrationSymbol = compilationContext.Compilation
                .GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Migrations.Migration");
            if (migrationSymbol is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, migrationSymbol),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (memberAccess.Name.Identifier.Text != "AddColumn")
        {
            return;
        }

        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.ContainingType.ToDisplayString()
            != "Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder")
        {
            return;
        }

        INamedTypeSymbol? migrationClass =
            MigrationAnalyzerHelpers.GetContainingClass(invocation, context.SemanticModel);
        if (migrationClass is null)
        {
            return;
        }

        if (!MigrationAnalyzerHelpers.InheritsFromMigration(migrationClass, migrationBase))
        {
            return;
        }

        ArgumentListSyntax argList = invocation.ArgumentList;

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
            Diagnostic.Create(Rule, invocation.GetLocation(), migrationClass.Name));
    }
}
