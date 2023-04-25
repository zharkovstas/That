using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace That.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class SpecifyAssertionMessageCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = ImmutableArray.Create(Descriptors.That0001.Id);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var node = root?.FindNode(context.Span);

        if (node is not InvocationExpressionSyntax invocation) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Auto-generate assertion message",
                createChangedDocument: cancellationToken => FixAsync(
                    context.Document,
                    invocation,
                    cancellationToken),
                equivalenceKey: "Auto-generate assertion message"),
            context.Diagnostics);
    }

    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    private static async Task<Document> FixAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var condition = invocation.ArgumentList.Arguments[0].Expression;

        var newArguments = invocation.ArgumentList.AddArguments(Argument(
            AssertionMessageProvider.Provide(condition).NormalizeWhitespace()
        ));

        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
        if (oldRoot == null)
        {
            return document;
        }

        var newRoot = oldRoot.ReplaceNode(invocation.ArgumentList, newArguments);

        return document.WithSyntaxRoot(newRoot);
    }
}