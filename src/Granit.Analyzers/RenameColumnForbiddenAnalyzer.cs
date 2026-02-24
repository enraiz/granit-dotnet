using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// GR-MIGA002 — Reports an error unconditionally when <c>RenameColumn</c> is called inside
/// an EF Core <c>Migration</c> class.
/// </summary>
/// <remarks>
/// <c>RenameColumn</c> is never safe for zero-downtime deployments because it breaks the running
/// application. Use <c>AddColumn</c> (Expand phase) + data migration + <c>DropColumn</c>
/// (Contract phase) instead.
/// <para>
/// Unlike GR-MIGA001, this rule activates whenever EF Core migrations are present in the
/// compilation, regardless of whether <c>Granit.Persistence.Migrations</c> is referenced.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RenameColumnForbiddenAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "RenameColumn is not zero-downtime safe",
        messageFormat: "RenameColumn is forbidden in the Expand & Contract pattern. "
            + "Use AddColumn (Expand) followed by DropColumn (Contract) instead.",
        category: "Migrations",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RenameColumn breaks the running application during deployment. "
            + "Use AddColumn (Expand phase) + data migration + DropColumn (Contract phase) instead.");

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
            // Activate whenever EF Core migrations are referenced (no Granit package required).
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

        if (memberAccess.Name.Identifier.Text != "RenameColumn")
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

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }
}
