using System.Collections.Immutable;
using System.Composition;
using System.Text;

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
                    return CreateInterpolatedMessage(
                        binaryExpression.Left,
                        binaryExpression.Right,
                        expectedModifier: null);
                case SyntaxKind.NotEqualsExpression:
                    return CreateInterpolatedMessage(
                        binaryExpression.Left,
                        binaryExpression.Right,
                        expectedModifier: "not");
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return CreateInterpolatedMessage(
                        binaryExpression.Left,
                        binaryExpression.Right,
                        expectedModifier: binaryExpression.OperatorToken.ToString());
                default:
                    break;
            }
        }

        if (condition is PrefixUnaryExpressionSyntax prefixUnaryExpression)
        {
            switch ((SyntaxKind)prefixUnaryExpression.RawKind)
            {
                case SyntaxKind.LogicalNotExpression:
                    return LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal($"Expected: {prefixUnaryExpression.Operand.WithoutTrivia()} to be false"));
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
                        return CreateInterpolatedMessage(
                            memberAccessExpression.Expression,
                            firstArgumentExpression,
                            expectedModifier: null);
                    case nameof(string.StartsWith):
                        return CreateInterpolatedMessage(
                            memberAccessExpression.Expression,
                            firstArgumentExpression,
                            expectedModifier: "starts with");
                    case nameof(string.EndsWith):
                        return CreateInterpolatedMessage(
                            memberAccessExpression.Expression,
                            firstArgumentExpression,
                            expectedModifier: "ends with");
                    case nameof(string.Contains):
                        return CreateInterpolatedMessage(
                            memberAccessExpression.Expression,
                            firstArgumentExpression,
                            expectedModifier: "contains");
                    default:
                        break;
                }
            }
            else
            {
                switch (memberAccessExpression.Name.ToString())
                {
                    case nameof(Enumerable.Any):
                        return LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal($"{memberAccessExpression.Expression}; Expected: not <empty>; But was: <empty>"));
                    default:
                        break;
                }
            }
        }

        if (condition is IsPatternExpressionSyntax isPatternExpression)
        {
            return CreateInterpolatedMessage(
                isPatternExpression.Expression,
                isPatternExpression.Pattern,
                expectedModifier: null
            );
        }

        return LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            Literal($"Expected: {condition.WithoutTrivia()}"));
    }

    private static InterpolatedStringExpressionSyntax CreateInterpolatedMessage(
        ExpressionSyntax actualExpression,
        ExpressionOrPatternSyntax expectedExpressionOrPattern,
        string? expectedModifier)
    {
        var firstContentBuilder = new StringBuilder($"{actualExpression.WithoutTrivia()}; Expected: ");
        if (expectedModifier != null)
        {
            firstContentBuilder.Append($"{expectedModifier} ");
        }

        if (expectedExpressionOrPattern is not ExpressionSyntax)
        {
            firstContentBuilder.Append($"{expectedExpressionOrPattern.WithoutTrivia()}; But was: ");
        }

        var firstContent = firstContentBuilder
            .ToString()
            .Replace("{", "{{")
            .Replace("}", "}}")
            .Replace(@"\", @"\\")
            .Replace(@"""", @"\""");

        var interpolatedStringContents = new List<InterpolatedStringContentSyntax>
        {
            InterpolatedStringText().WithTextToken(Token(
                TriviaList(),
                SyntaxKind.InterpolatedStringTextToken,
                firstContent,
                firstContent,
                TriviaList()))
        };

        if (expectedExpressionOrPattern is ExpressionSyntax expectedExpression)
        {
            interpolatedStringContents.Add(Interpolation(expectedExpression));
            interpolatedStringContents.Add(InterpolatedStringText().WithTextToken(Token(
                TriviaList(),
                SyntaxKind.InterpolatedStringTextToken,
                "; But was: ",
                "; But was: ",
                TriviaList())));
        }

        interpolatedStringContents.Add(Interpolation(actualExpression.WithoutTrivia()));

        return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(List(interpolatedStringContents));
    }
}