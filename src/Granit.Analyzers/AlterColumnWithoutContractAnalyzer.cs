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
            (INamedTypeSymbol CycleAttr, INamedTypeSymbol Migration)? symbols =
                MigrationAnalyzerHelpers.ResolveGranitMigrationSymbols(compilationContext.Compilation);
            if (symbols is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, symbols.Value.Migration, symbols.Value.CycleAttr),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase,
        INamedTypeSymbol cycleAttrType)
    {
        (INamedTypeSymbol MigrationClass, IMethodSymbol Method)? result =
            MigrationAnalyzerHelpers.TryGetMigrationInvocation(context, migrationBase, "AlterColumn");
        if (result is null)
        {
            return;
        }

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Find the oldClrType argument — if absent, this is a constraint-only change.
        ArgumentSyntax? oldClrTypeArg =
            MigrationAnalyzerHelpers.FindNamedArgument(invocation.ArgumentList, "oldClrType");
        if (oldClrTypeArg is null)
        {
            return;
        }

        // Compare oldClrType with the generic type argument T of AlterColumn<T>.
        if (result.Value.Method.TypeArguments.Length == 0)
        {
            return;
        }

        ITypeSymbol targetType = result.Value.Method.TypeArguments[0];

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
        if (MigrationAnalyzerHelpers.HasContractAnnotation(result.Value.MigrationClass, cycleAttrType))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, invocation.GetLocation(), result.Value.MigrationClass.Name));
    }
}
