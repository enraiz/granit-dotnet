using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Granit.Analyzers;

/// <summary>
/// Shared helpers for migration Roslyn analyzers.
/// </summary>
internal static class MigrationAnalyzerHelpers
{
    /// <summary>Integer value of <c>MigrationPhase.Contract</c>.</summary>
    private const int ContractPhaseValue = 2;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="type"/> inherits (directly or indirectly)
    /// from <paramref name="migrationBase"/>.
    /// </summary>
    internal static bool InheritsFromMigration(INamedTypeSymbol type, INamedTypeSymbol migrationBase)
    {
        INamedTypeSymbol? current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, migrationBase))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="classSymbol"/> is annotated with
    /// <c>[MigrationCycle(MigrationPhase.Contract, ...)]</c>.
    /// </summary>
    internal static bool HasContractAnnotation(INamedTypeSymbol classSymbol, INamedTypeSymbol cycleAttrType)
    {
        foreach (AttributeData attr in classSymbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, cycleAttrType))
            {
                continue;
            }

            if (attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is int phase
                && phase == ContractPhaseValue)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks up the syntax tree from <paramref name="node"/> and returns the
    /// <see cref="INamedTypeSymbol"/> of the immediately enclosing class declaration,
    /// or <see langword="null"/> if none is found.
    /// </summary>
    internal static INamedTypeSymbol? GetContainingClass(SyntaxNode node, SemanticModel semanticModel)
    {
        SyntaxNode? current = node.Parent;
        while (current is not null)
        {
            if (current is ClassDeclarationSyntax classDecl)
            {
                return semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the argument list contains a named argument <paramref name="name"/>
    /// whose constant value evaluates to <see langword="true"/>.
    /// </summary>
    internal static bool HasNamedArgumentWithTrueValue(ArgumentListSyntax argList, string name, SemanticModel semanticModel)
    {
        foreach (ArgumentSyntax arg in argList.Arguments)
        {
            if (arg.NameColon?.Name.Identifier.Text != name)
            {
                continue;
            }

            Microsoft.CodeAnalysis.Optional<object?> constant = semanticModel.GetConstantValue(arg.Expression);
            return constant.HasValue && constant.Value is true;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the argument list contains a named argument with the given <paramref name="name"/>.
    /// </summary>
    internal static bool HasNamedArgument(ArgumentListSyntax argList, string name)
    {
        foreach (ArgumentSyntax arg in argList.Arguments)
        {
            if (arg.NameColon?.Name.Identifier.Text == name)
            {
                return true;
            }
        }

        return false;
    }
}
