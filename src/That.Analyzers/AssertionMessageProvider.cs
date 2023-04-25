using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace That.Analyzers;

internal static class AssertionMessageProvider
{
    internal static ExpressionSyntax Provide(ExpressionSyntax condition)
    {
        switch (condition)
        {
            case BinaryExpressionSyntax binaryExpression:
                if (TryProvide(binaryExpression, out ExpressionSyntax message)) return message;
                break;
            case PrefixUnaryExpressionSyntax prefixUnaryExpression:
                if (TryProvide(prefixUnaryExpression, out message)) return message;
                break;
            case InvocationExpressionSyntax invocationExpression:
                if (TryProvide(invocationExpression, out message)) return message;
                break;
            case CastExpressionSyntax castExpression:
                if (TryProvide(castExpression, out message)) return message;
                break;
            case IsPatternExpressionSyntax isPatternExpression:
                return Provide(isPatternExpression);
            case ParenthesizedExpressionSyntax parenthesizedExpression:
                return Provide(parenthesizedExpression.Expression);
            default:
                break;
        }

        return new InterpolatedStringBuilder()
            .AppendText("Expected: ")
            .AppendSyntaxAsText(condition);
    }

    private static bool TryProvide(
        BinaryExpressionSyntax expression,
        out ExpressionSyntax message)
    {
        message = default!;
        switch ((SyntaxKind)expression.RawKind)
        {
            case SyntaxKind.EqualsExpression:
                message = new InterpolatedStringBuilder()
                    .AppendSyntaxAsText(expression.Left)
                    .AppendText("; Expected: ")
                    .AppendExpression(expression.Right)
                    .AppendText("; But was: ")
                    .AppendExpression(expression.Left);
                return true;
            case SyntaxKind.NotEqualsExpression:
                message = new InterpolatedStringBuilder()
                    .AppendSyntaxAsText(expression.Left)
                    .AppendText("; Expected: not ")
                    .AppendExpression(expression.Right)
                    .AppendText("; But was: ")
                    .AppendExpression(expression.Left);
                return true;
            case SyntaxKind.LessThanExpression:
            case SyntaxKind.LessThanOrEqualExpression:
            case SyntaxKind.GreaterThanExpression:
            case SyntaxKind.GreaterThanOrEqualExpression:
                message = new InterpolatedStringBuilder()
                    .AppendSyntaxAsText(expression.Left)
                    .AppendText($"; Expected: {expression.OperatorToken} ")
                    .AppendExpression(expression.Right)
                    .AppendText("; But was: ")
                    .AppendExpression(expression.Left);
                return true;
            default:
                return false;
        }
    }

    private static bool TryProvide(
        PrefixUnaryExpressionSyntax expression,
        out ExpressionSyntax message)
    {
        message = default!;
        switch ((SyntaxKind)expression.RawKind)
        {
            case SyntaxKind.LogicalNotExpression:
                message = new InterpolatedStringBuilder()
                    .AppendText("Expected: ")
                    .AppendSyntaxAsText(expression.Operand)
                    .AppendText(" to be false");
                return true;
            default:
                return false;
        }
    }

    private static bool TryProvide(
        InvocationExpressionSyntax expression,
        out ExpressionSyntax message)
    {
        message = default!;
        var arguments = expression.ArgumentList.Arguments;

        if (expression.Expression is IdentifierNameSyntax identifierName)
        {
            if (expression.ArgumentList.Arguments.Count > 1)
            {
                var firstArgumentExpression = expression.ArgumentList.Arguments[0].Expression;
                var secondArgumentExpression = expression.ArgumentList.Arguments[1].Expression;

                switch (identifierName.ToString())
                {
                    case nameof(object.Equals):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(firstArgumentExpression)
                            .AppendText("; Expected: ")
                            .AppendExpression(secondArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(firstArgumentExpression);
                        return true;
                    case nameof(object.ReferenceEquals):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(firstArgumentExpression)
                            .AppendText("; Expected: same as ")
                            .AppendExpression(secondArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(firstArgumentExpression);
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        if (expression.Expression is MemberAccessExpressionSyntax memberAccessExpression
            && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            switch (memberAccessExpression.Name.ToString())
            {
                case nameof(object.Equals):
                    if (memberAccessExpression.Expression is PredefinedTypeSyntax && arguments.Count > 1)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(arguments[0].Expression)
                            .AppendText("; Expected: ")
                            .AppendExpression(arguments[1].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(arguments[0].Expression);
                        return true;
                    }
                    if (arguments.Count > 0)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: ")
                            .AppendExpression(arguments[0].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    }
                    break;
                case nameof(object.ReferenceEquals):
                    if (memberAccessExpression.Expression is PredefinedTypeSyntax && arguments.Count > 1)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(arguments[0].Expression)
                            .AppendText("; Expected: same as ")
                            .AppendExpression(arguments[1].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(arguments[0].Expression);
                        return true;
                    }
                    if (arguments.Count > 0)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: same as ")
                            .AppendExpression(arguments[0].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    }
                    break;
                case nameof(Enumerable.SequenceEqual):
                    if (memberAccessExpression.Expression.ToString() == nameof(Enumerable) && arguments.Count > 1)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(arguments[0].Expression)
                            .AppendText("; Expected: ")
                            .AppendExpression(arguments[1].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(arguments[0].Expression);
                        return true;
                    }
                    if (arguments.Count > 0)
                    {
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: ")
                            .AppendExpression(arguments[0].Expression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    }
                    break;
                default:
                    break;
            }

            if (expression.ArgumentList.Arguments.Count == 1)
            {
                var firstArgumentExpression = expression.ArgumentList.Arguments[0].Expression;

                switch (memberAccessExpression.Name.ToString())
                {
                    case nameof(string.StartsWith):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: starts with ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    case nameof(string.EndsWith):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: ends with ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    case nameof(string.Contains):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: contains ")
                            .AppendExpression(firstArgumentExpression)
                            .AppendText("; But was: ")
                            .AppendExpression(memberAccessExpression.Expression);
                        return true;
                    default:
                        return false;
                }
            }

            if (expression.ArgumentList.Arguments.Count == 0)
            {
                switch (memberAccessExpression.Name.ToString())
                {
                    case nameof(Enumerable.Any):
                        message = new InterpolatedStringBuilder()
                            .AppendSyntaxAsText(memberAccessExpression.Expression)
                            .AppendText("; Expected: not <empty>; But was: <empty>");
                        return true;
                    default:
                        return false;
                }
            }
        }
        return false;
    }

    private static ExpressionSyntax Provide(IsPatternExpressionSyntax expression)
    {
        return new InterpolatedStringBuilder()
            .AppendSyntaxAsText(expression.Expression)
            .AppendText("; Expected: ")
            .AppendSyntaxAsText(expression.Pattern)
            .AppendText("; But was: ")
            .AppendExpression(expression.Expression);
    }

    private static bool TryProvide(
        CastExpressionSyntax expression,
        out ExpressionSyntax message)
    {
        message = default!;
        if (expression.Type is PredefinedTypeSyntax predefinedTypeSyntax
            && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.BoolKeyword))
        {
            message = new InterpolatedStringBuilder()
                .AppendText("Expected: ")
                .AppendSyntaxAsText(expression.Expression);
            return true;
        }
        return false;
    }
}