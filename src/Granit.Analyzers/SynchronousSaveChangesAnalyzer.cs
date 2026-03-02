using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// GR-EF001 — Reports a warning when <c>SaveChanges()</c> is called on a
/// <c>DbContext</c> instead of <c>SaveChangesAsync()</c>.
/// </summary>
/// <remarks>
/// Synchronous <c>SaveChanges()</c> blocks the calling thread, which depletes
/// the thread pool under load. Use <c>SaveChangesAsync()</c> in all async contexts.
/// Opt-in: only activates when <c>Microsoft.EntityFrameworkCore.DbContext</c> is
/// present in the compilation.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SynchronousSaveChangesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic identifier.</summary>
    public const string DiagnosticId = "GREF001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use SaveChangesAsync() instead of SaveChanges()",
        messageFormat: "Use SaveChangesAsync() instead of SaveChanges() to avoid blocking the thread pool",
        category: "EntityFramework",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Synchronous SaveChanges() blocks the calling thread and can deplete "
            + "the thread pool under load. Use SaveChangesAsync() instead.");

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
            // Opt-in: only activate when EF Core DbContext is referenced.
            INamedTypeSymbol? dbContextSymbol = compilationContext.Compilation
                .GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            if (dbContextSymbol is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, dbContextSymbol),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol dbContextBase)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (memberAccess.Name.Identifier.Text != "SaveChanges")
        {
            return;
        }

        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!InheritsFromOrEquals(methodSymbol.ContainingType, dbContextBase))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    private static bool InheritsFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        INamedTypeSymbol? current = type;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
