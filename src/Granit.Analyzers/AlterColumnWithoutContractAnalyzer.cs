using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// GR-MIGA004 — Reports a warning when <c>AlterColumn</c> is called with an <c>oldClrType</c>
/// argument that differs from the target type argument, and the migration class is not annotated
/// with <c>[MigrationCycle(MigrationPhase.Contract, ...)]</c>.
/// </summary>
/// <remarks>
/// Changing a column's CLR type (e.g., <c>int</c> → <c>string</c>) is a breaking schema change
/// that requires careful coordination in the Expand &amp; Contract pattern.
/// When <c>oldClrType</c> is absent (constraint-only modification), the rule does not fire.
/// Opt-in: only activates when <c>Granit.Persistence.Migrations</c> is referenced.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AlterColumnWithoutContractAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "AlterColumn with a type change requires a Contract-phase annotation",
        messageFormat: "Migration '{0}' changes a column type without [MigrationCycle(MigrationPhase.Contract, ...)]. "
            + "Type changes must be performed in the Contract phase.",
        category: "Migrations",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Changing a column's CLR type is a breaking change that must be coordinated "
            + "through the Expand & Contract pattern. Annotate the migration class with "
            + "[MigrationCycle(MigrationPhase.Contract, \"cycle-id\")] to suppress this warning.");

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

        if (memberAccess.Name.Identifier.Text != "AlterColumn")
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

        // Find the oldClrType argument — if absent, this is a constraint-only change.
        ArgumentSyntax? oldClrTypeArg = FindNamedArgument(invocation.ArgumentList, "oldClrType");
        if (oldClrTypeArg is null)
        {
            return;
        }

        // Compare oldClrType with the generic type argument T of AlterColumn<T>.
        if (methodSymbol.TypeArguments.Length == 0)
        {
            return;
        }

        ITypeSymbol targetType = methodSymbol.TypeArguments[0];

        // oldClrType must be typeof(SomeType) to be statically comparable.
        if (oldClrTypeArg.Expression is not TypeOfExpressionSyntax typeofExpr)
        {
            return;
        }

        ITypeSymbol? oldType = context.SemanticModel.GetTypeInfo(typeofExpr.Type).Type;
        if (oldType is null)
        {
            return;
        }

        // Same type → not a type change, no warning.
        if (SymbolEqualityComparer.Default.Equals(targetType, oldType))
        {
            return;
        }

        // Type changed — require Contract annotation.
        if (MigrationAnalyzerHelpers.HasContractAnnotation(migrationClass, cycleAttrType))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, invocation.GetLocation(), migrationClass.Name));
    }

    private static ArgumentSyntax? FindNamedArgument(ArgumentListSyntax argList, string name)
    {
        foreach (ArgumentSyntax arg in argList.Arguments)
        {
            if (arg.NameColon?.Name.Identifier.Text == name)
            {
                return arg;
            }
        }

        return null;
    }
}
