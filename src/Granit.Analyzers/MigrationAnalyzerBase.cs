using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Granit.Analyzers;

/// <summary>
/// Abstract base for migration analyzers. Provides the <see cref="SupportedDiagnostics"/>
/// property and the <see cref="Initialize"/> scaffold (concurrent execution, no generated code).
/// </summary>
public abstract class MigrationAnalyzerBase : DiagnosticAnalyzer
{
    /// <summary>Gets the diagnostic descriptor for this analyzer.</summary>
    protected abstract DiagnosticDescriptor Rule { get; }

    /// <inheritdoc/>
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public sealed override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    /// <summary>
    /// Resolves required symbols and registers the syntax node action.
    /// </summary>
    protected abstract void OnCompilationStart(CompilationStartAnalysisContext context);
}

/// <summary>
/// Base for migration analyzers that opt in to <c>Granit.Persistence.Migrations</c>.
/// Resolves <c>MigrationCycleAttribute</c> and the EF Core <c>Migration</c> base class,
/// then delegates to <see cref="AnalyzeNode"/>.
/// </summary>
public abstract class GranitMigrationAnalyzerBase : MigrationAnalyzerBase
{
    /// <inheritdoc/>
    protected sealed override void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        (INamedTypeSymbol CycleAttr, INamedTypeSymbol Migration)? symbols =
            MigrationAnalyzerHelpers.ResolveGranitMigrationSymbols(context.Compilation);
        if (symbols is null)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            nodeContext => AnalyzeNode(nodeContext, symbols.Value.Migration, symbols.Value.CycleAttr),
            SyntaxKind.InvocationExpression);
    }

    /// <summary>Analyzes an invocation expression inside a Granit migration.</summary>
    protected abstract void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase,
        INamedTypeSymbol cycleAttrType);
}

/// <summary>
/// Base for migration analyzers that require only EF Core (no Granit opt-in).
/// Resolves the EF Core <c>Migration</c> base class symbol, then delegates to
/// <see cref="AnalyzeNode"/>.
/// </summary>
public abstract class EfCoreMigrationAnalyzerBase : MigrationAnalyzerBase
{
    /// <inheritdoc/>
    protected sealed override void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        INamedTypeSymbol? migrationSymbol =
            MigrationAnalyzerHelpers.ResolveEfCoreMigrationSymbol(context.Compilation);
        if (migrationSymbol is null)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            nodeContext => AnalyzeNode(nodeContext, migrationSymbol),
            SyntaxKind.InvocationExpression);
    }

    /// <summary>Analyzes an invocation expression inside an EF Core migration.</summary>
    protected abstract void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol migrationBase);
}
