using System.Composition;
using Granit.Analyzers.CodeFixes.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Granit.Analyzers.CodeFixes;

/// <summary>
/// CodeFix for GRSEC002 — replaces <c>Guid.NewGuid()</c> with <c>_guidGenerator.Create()</c>
/// and injects <c>IGuidGenerator</c> via constructor DI.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GuidNewGuidCodeFixProvider))]
[Shared]
public sealed class GuidNewGuidCodeFixProvider : DependencyInjectionCodeFixProviderBase
{
    protected override string Title => "Replace with IGuidGenerator injection";
    protected override string DiagnosticId => GuidNewGuidAnalyzer.DiagnosticId;
    protected override string InterfaceName => "IGuidGenerator";
    protected override string FieldName => "_guidGenerator";
    protected override string ParamName => "guidGenerator";
    protected override string UsingNamespace => "Granit.Guids";

    protected override bool IsExpectedNodeType(SyntaxNode node) =>
        node is InvocationExpressionSyntax;

    protected override SyntaxNode BuildReplacement(SyntaxNode trackedNode) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(FieldName),
                SyntaxFactory.IdentifierName("Create")));

    protected override bool ShouldInjectDependency(SyntaxNode originalNode) =>
        !DependencyInjectionHelper.IsInStaticContext(originalNode);
}
