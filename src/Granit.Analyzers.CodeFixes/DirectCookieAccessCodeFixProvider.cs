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
/// CodeFix for GRSEC004 — replaces direct <c>IResponseCookies.Append()</c> / <c>Delete()</c>
/// with <c>IGranitCookieManager</c> calls and injects the dependency via constructor DI.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DirectCookieAccessCodeFixProvider))]
[Shared]
public sealed class DirectCookieAccessCodeFixProvider : CodeFixProvider
{
    private const string Title = "Replace with IGranitCookieManager injection";
    private const string InterfaceName = "IGranitCookieManager";
    private const string FieldName = "_cookieManager";
    private const string ParamName = "cookieManager";
    private const string UsingNamespace = "Granit.Cookies";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DirectCookieAccessAnalyzer.DiagnosticId);

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

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)node;
        MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        string methodName = memberAccess.Name.Identifier.Text;

        // Annotate the target node
        SyntaxAnnotation annotation = new();
        SyntaxNode annotatedNode = node.WithAdditionalAnnotations(annotation);
        root = root.ReplaceNode(node, annotatedNode);

        SyntaxNode? trackedNode = root.GetAnnotatedNodes(annotation).FirstOrDefault();
        if (trackedNode is null)
        {
            return document;
        }

        // Build replacement based on method name
        InvocationExpressionSyntax tracked = (InvocationExpressionSyntax)trackedNode;
        ArgumentListSyntax originalArgs = tracked.ArgumentList;

        ExpressionSyntax replacement;
        if (methodName == "Append")
        {
            // Cookies.Append(key, value) → await _cookieManager.SetCookieAsync(httpContext, key, value)
            SeparatedSyntaxList<ArgumentSyntax> newArgs = SyntaxFactory.SeparatedList(
                new[]
                {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("httpContext")),
                    originalArgs.Arguments.ElementAtOrDefault(0) ?? SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))),
                    originalArgs.Arguments.ElementAtOrDefault(1) ?? SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("")))
                });

            replacement = SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(FieldName),
                        SyntaxFactory.IdentifierName("SetCookieAsync")),
                    SyntaxFactory.ArgumentList(newArgs)));
        }
        else
        {
            // Cookies.Delete(key) → _cookieManager.DeleteCookie(httpContext, key)
            SeparatedSyntaxList<ArgumentSyntax> newArgs = SyntaxFactory.SeparatedList(
                new[]
                {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("httpContext")),
                    originalArgs.Arguments.ElementAtOrDefault(0) ?? SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("")))
                });

            replacement = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(FieldName),
                    SyntaxFactory.IdentifierName("DeleteCookie")),
                SyntaxFactory.ArgumentList(newArgs));
        }

        root = root.ReplaceNode(trackedNode, replacement.WithTriviaFrom(trackedNode));

        // Find the containing class and inject DI
        ClassDeclarationSyntax? classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDecl is not null)
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

        // Add using directive
        if (root is CompilationUnitSyntax compilationUnit)
        {
            root = DependencyInjectionHelper.EnsureUsingDirective(compilationUnit, UsingNamespace);
        }

        return document.WithSyntaxRoot(root);
    }
}
