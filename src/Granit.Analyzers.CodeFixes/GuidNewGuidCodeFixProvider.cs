using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Granit.Analyzers.CodeFixes.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
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
public sealed class GuidNewGuidCodeFixProvider : CodeFixProvider
{
    private const string Title = "Replace with IGuidGenerator injection";
    private const string InterfaceName = "IGuidGenerator";
    private const string FieldName = "_guidGenerator";
    private const string ParamName = "guidGenerator";
    private const string UsingNamespace = "Granit.Guids";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(GuidNewGuidAnalyzer.DiagnosticId);

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

        if (node is not InvocationExpressionSyntax)
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

        // Annotate the target node
        SyntaxAnnotation annotation = new();
        SyntaxNode annotatedNode = node.WithAdditionalAnnotations(annotation);
        root = root.ReplaceNode(node, annotatedNode);

        // 1. Replace Guid.NewGuid() with _guidGenerator.Create()
        SyntaxNode? trackedNode = root.GetAnnotatedNodes(annotation).FirstOrDefault();
        if (trackedNode is null)
        {
            return document;
        }

        InvocationExpressionSyntax replacement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(FieldName),
                SyntaxFactory.IdentifierName("Create")))
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
                return method.Modifiers.Any(SyntaxKind.StaticKeyword);
            }

            if (ancestor is PropertyDeclarationSyntax property)
            {
                return property.Modifiers.Any(SyntaxKind.StaticKeyword);
            }

            if (ancestor is ClassDeclarationSyntax)
            {
                break;
            }
        }

        return false;
    }
}
