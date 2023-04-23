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
            GetMessage(condition).NormalizeWhitespace()
        ));

        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
        if (oldRoot == null)
        {
            return document;
        }

        var newRoot = oldRoot.ReplaceNode(invocation.ArgumentList, newArguments);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax GetMessage(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binaryExpression)
        {
            switch ((SyntaxKind)binaryExpression.RawKind)
            {
                case SyntaxKind.EqualsExpression:
                    return new InterpolatedStringBuilder()
                        .AppendSyntaxAsText(binaryExpression.Left)
                        .AppendText("; Expected: ")
                        .AppendExpression(binaryExpression.Right)
                        .AppendText("; But was: ")
                        .AppendExpression(binaryExpression.Left);
                case SyntaxKind.NotEqualsExpression:
                    return new InterpolatedStringBuilder()
                        .AppendSyntaxAsText(binaryExpression.Left)
                        .AppendText("; Expected: not ")
                        .AppendExpression(binaryExpression.Right)
                        .AppendText("; But was: ")
                        .AppendExpression(binaryExpression.Left);
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return new InterpolatedStringBuilder()
                        .AppendSyntaxAsText(binaryExpression.Left)
                        .AppendText($"; Expected: {binaryExpression.OperatorToken} ")
                        .AppendExpression(binaryExpression.Right)
                        .AppendText("; But was: ")
                        .AppendExpression(binaryExpression.Left);
                default:
                    break;
            }
        }

        if (condition is PrefixUnaryExpressionSyntax prefixUnaryExpression)
        {
            switch ((SyntaxKind)prefixUnaryExpression.RawKind)
            {
                case SyntaxKind.LogicalNotExpression:
                    return new InterpolatedStringBuilder()
                        .AppendText("Expected: ")
                        .AppendSyntaxAsText(prefixUnaryExpression.Operand)
                        .AppendText(" to be false");
                default:
                    break;
            }
        }

        if (condition is InvocationExpressionSyntax invocationExpression
            && invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression
            && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            if (invocationExpression.ArgumentList.Arguments.Count > 0)
            {
                var firstArgumentExpression = invocationExpression.ArgumentList.Arguments[0].Expression;

                switch (memberAccessExpression.Name.ToString())
                {
                    case nameof(object.Equals):
                    case nameof(Enumerable.SequenceEqual):
                        return new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                    case nameof(string.StartsWith):
                        return new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: starts with ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                    case nameof(string.EndsWith):
                        return new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: ends with ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                    case nameof(string.Contains):
                        return new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: contains ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                    default:
                        break;
                }
            }
            else
            {
                switch (memberAccessExpression.Name.ToString())
                {
                    case nameof(Enumerable.Any):
                        return new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: not <empty>; But was: <empty>");
                    default:
                        break;
                }
            }
        }

        if (condition is IsPatternExpressionSyntax isPatternExpression)
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(isPatternExpression.Expression)
                .AppendText("; Expected: ")
                .AppendSyntaxAsText(isPatternExpression.Pattern)
                .AppendText("; But was: ")
                .AppendExpression(isPatternExpression.Expression);
        }

        return new InterpolatedStringBuilder()
            .AppendText("Expected: ")
            .AppendSyntaxAsText(condition);
    }
}