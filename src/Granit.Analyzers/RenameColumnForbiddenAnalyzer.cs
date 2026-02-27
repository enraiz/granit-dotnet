using Microsoft.CodeAnalysis;
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
public sealed class RenameColumnForbiddenAnalyzer : EfCoreMigrationAnalyzerBase
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GRMIGA002";

    private static readonly DiagnosticDescriptor _rule = new(
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
    protected override DiagnosticDescriptor Rule => _rule;

    /// <inheritdoc/>
    protected override void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase)
    {
        (INamedTypeSymbol MigrationClass, IMethodSymbol Method)? result =
            MigrationAnalyzerHelpers.TryGetMigrationInvocation(context, migrationBase, "RenameColumn");
        if (result is null)
        {
            return;
        }

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        context.ReportDiagnostic(Diagnostic.Create(_rule, invocation.GetLocation()));
    }
}
