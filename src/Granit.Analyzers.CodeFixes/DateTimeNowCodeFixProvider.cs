using System.Composition;
using Granit.Analyzers.CodeFixes.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Granit.Analyzers.CodeFixes;

/// <summary>
/// CodeFix for GRSEC001 — replaces <c>DateTime.Now</c> / <c>DateTimeOffset.UtcNow</c>
/// with <c>_clock.Now</c> and injects <c>IClock</c> via constructor DI.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeNowCodeFixProvider))]
[Shared]
public sealed class DateTimeNowCodeFixProvider : DependencyInjectionCodeFixProviderBase
{
    protected override string Title => "Replace with IClock injection";
    protected override string DiagnosticId => DateTimeNowAnalyzer.DiagnosticId;
    protected override string InterfaceName => "IClock";
    protected override string FieldName => "_clock";
    protected override string ParamName => "clock";
    protected override string UsingNamespace => "Granit.Timing";

    protected override bool IsExpectedNodeType(SyntaxNode node) =>
        node is MemberAccessExpressionSyntax;

    protected override SyntaxNode BuildReplacement(SyntaxNode trackedNode) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(FieldName),
            SyntaxFactory.IdentifierName("Now"));

    protected override bool ShouldInjectDependency(SyntaxNode originalNode) =>
        !DependencyInjectionHelper.IsInStaticContext(originalNode);
}
