using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Granit.Analyzers.CodeFixes.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Granit.Analyzers.CodeFixes;

/// <summary>
/// CodeFix for GRSEC001 — replaces <c>DateTime.Now</c> / <c>DateTimeOffset.UtcNow</c>
/// with <c>_clock.Now</c> and injects <c>IClock</c> via constructor DI.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeNowCodeFixProvider))]
[Shared]
public sealed class DateTimeNowCodeFixProvider : CodeFixProvider
{
    private const string Title = "Replace with IClock injection";
    private const string InterfaceName = "IClock";
    private const string FieldName = "_clock";
    private const string ParamName = "clock";
    private const string UsingNamespace = "Granit.Timing";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DateTimeNowAnalyzer.DiagnosticId);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan);

        if (node is not MemberAccessExpressionSyntax)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => ApplyFixAsync(context.Document, node, ct),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode node,
        CancellationToken ct)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(ct);
        if (root is null)
        {
            return document;
        }

        // Annotate the target node to find it after mutations
        SyntaxAnnotation annotation = new();
        SyntaxNode annotatedNode = node.WithAdditionalAnnotations(annotation);
        root = root.ReplaceNode(node, annotatedNode);

        // 1. Replace the expression with _clock.Now
        SyntaxNode? trackedNode = root.GetAnnotatedNodes(annotation).FirstOrDefault();
        if (trackedNode is null)
        {
            return document;
        }

        MemberAccessExpressionSyntax replacement = Microsoft.CodeAnalysis.CSharp.SyntaxFactory
            .MemberAccessExpression(
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.IdentifierName(FieldName),
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.IdentifierName("Now"))
            .WithTriviaFrom(trackedNode);

        root = root.ReplaceNode(trackedNode, replacement);

        // 2. Find the containing class and inject DI (only for instance contexts)
        ClassDeclarationSyntax? classDecl = replacement.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (classDecl is not null && !IsInStaticContext(node))
        {
            SyntaxAnnotation classAnnotation = new();
            root = root.ReplaceNode(classDecl, classDecl.WithAdditionalAnnotations(classAnnotation));

            ClassDeclarationSyntax? trackedClass = root.GetAnnotatedNodes(classAnnotation)
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (trackedClass is not null)
            {
                ClassDeclarationSyntax modifiedClass = DependencyInjectionHelper.EnsureFieldExists(
                    trackedClass, InterfaceName, FieldName);
                modifiedClass = DependencyInjectionHelper.EnsureConstructorParameter(
                    modifiedClass, InterfaceName, ParamName, FieldName);
                root = root.ReplaceNode(trackedClass, modifiedClass);
            }
        }

        // 3. Add using directive
        if (root is CompilationUnitSyntax compilationUnit)
        {
            root = DependencyInjectionHelper.EnsureUsingDirective(compilationUnit, UsingNamespace);
        }

        return document.WithSyntaxRoot(root);
    }

    private static bool IsInStaticContext(SyntaxNode node)
    {
        foreach (SyntaxNode? ancestor in node.Ancestors())
        {
            if (ancestor is MethodDeclarationSyntax method)
            {
                return method.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
            }

            if (ancestor is PropertyDeclarationSyntax property)
            {
                return property.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
            }

            if (ancestor is ClassDeclarationSyntax)
            {
                break;
            }
        }

        return false;
    }
}
