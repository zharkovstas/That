using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace That.Analyzers;

internal class InterpolatedStringBuilder
{
    private readonly List<object> contents = new();

    internal InterpolatedStringBuilder AppendText(string text)
    {
        contents.Add(text);
        return this;
    }

    internal InterpolatedStringBuilder AppendSyntaxAsText(SyntaxNode syntax)
    {
        contents.Add(syntax.WithoutTrivia().ToString());
        return this;
    }

    internal InterpolatedStringBuilder AppendExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                contents.Add(literal.Token.ValueText);
                break;
            case ParenthesizedExpressionSyntax parenthesizedExpression:
                return AppendExpression(parenthesizedExpression.Expression);
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExpression } invocationExpression:
                if (memberAccessExpression.Name.ToString() == nameof(object.ToString)
                    && invocationExpression.ArgumentList.Arguments.Count == 0)
                {
                    return AppendExpression(memberAccessExpression.Expression);
                }
                contents.Add(expression);
                break;
            default:
                contents.Add(expression);
                break;
        }

        return this;
    }

    public static implicit operator ExpressionSyntax(InterpolatedStringBuilder builder) => builder.Build();

    internal ExpressionSyntax Build()
    {
        if (contents.All(x => x is string))
        {
            return LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal(string.Join("", contents)));
        }

        var interpolatedStringContents = contents
            .Select(x => x switch
            {
                string s => InterpolatedStringText().WithTextToken(Token(
                    TriviaList(),
                    SyntaxKind.InterpolatedStringTextToken,
                    EscapeInterpolatedStringText(s),
                    EscapeInterpolatedStringText(s),
                    TriviaList())),
                ExpressionSyntax expression => (InterpolatedStringContentSyntax)Interpolation(expression.WithoutTrivia()),
                _ => throw new InvalidOperationException($"Invalid content type: {x}")
            });

        return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(List(interpolatedStringContents));
    }

    private static string EscapeInterpolatedStringText(string input)
    {
        return input
            .Replace("{", "{{")
            .Replace("}", "}}")
            .Replace(@"\", @"\\")
            .Replace(@"""", @"\""");
    }
}