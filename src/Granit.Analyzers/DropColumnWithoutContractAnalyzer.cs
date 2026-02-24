using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
public sealed class DropColumnWithoutContractAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "DropColumn requires a Contract-phase annotation",
        messageFormat: "Migration '{0}' calls DropColumn but is not annotated with [MigrationCycle(MigrationPhase.Contract, ...)]",
        category: "Migrations",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "DropColumn is only safe in the Contract phase of the Expand & Contract pattern. "
            + "Annotate the migration class with [MigrationCycle(MigrationPhase.Contract, \"cycle-id\")].");

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
                nodeContext => AnalyzeInvocation(nodeContext, migrationSymbol, cycleAttrSymbol),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase,
        INamedTypeSymbol cycleAttrType)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (memberAccess.Name.Identifier.Text != "DropColumn")
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

        if (MigrationAnalyzerHelpers.HasContractAnnotation(migrationClass, cycleAttrType))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, invocation.GetLocation(), migrationClass.Name));
    }
}
