using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace That.Analyzers;

internal static partial class AssertionMessageProvider
{
    internal static ExpressionSyntax Provide(ExpressionSyntax condition)
    {
        return condition switch
        {
            BinaryExpressionSyntax binaryExpression => Provide(binaryExpression),
            PrefixUnaryExpressionSyntax prefixUnaryExpression => Provide(prefixUnaryExpression),
            InvocationExpressionSyntax invocationExpression => Provide(invocationExpression),
            CastExpressionSyntax castExpression => Provide(castExpression),
            IsPatternExpressionSyntax isPatternExpression => Provide(isPatternExpression),
            ParenthesizedExpressionSyntax parenthesizedExpression => Provide(parenthesizedExpression.Expression),
            _ => ProvideDefault(condition),
        };
    }

    private static ExpressionSyntax Provide(BinaryExpressionSyntax condition)
    {
        var builder = new InterpolatedStringBuilder()
            .AppendSyntaxAsText(condition.Left)
            .AppendText("; Expected: ");

        return (SyntaxKind)condition.RawKind switch
        {
            SyntaxKind.EqualsExpression => builder
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.NotEqualsExpression => builder
                .AppendText("not ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.LessThanExpression
                or SyntaxKind.LessThanOrEqualExpression
                or SyntaxKind.GreaterThanExpression
                or SyntaxKind.GreaterThanOrEqualExpression => builder
                    .AppendText($"{condition.OperatorToken} ")
                    .AppendExpression(condition.Right)
                    .AppendText("; But was: ")
                    .AppendExpression(condition.Left),
            _ => ProvideDefault(condition),
        };
    }

    private static ExpressionSyntax Provide(PrefixUnaryExpressionSyntax expression)
    {
        return (SyntaxKind)expression.RawKind switch
        {
            SyntaxKind.LogicalNotExpression => new InterpolatedStringBuilder()
                .AppendText("Expected: ")
                .AppendSyntaxAsText(expression.Operand)
                .AppendText(" to be false"),
            _ => ProvideDefault(expression),
        };
    }

    private static ExpressionSyntax Provide(InvocationExpressionSyntax expression)
    {
        if (expression.Expression is not IdentifierNameSyntax and not MemberAccessExpressionSyntax)
        {
            return ProvideDefault(expression);
        }

        var methodName = expression.Expression switch
        {
            IdentifierNameSyntax identifier => identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            _ => throw new InvalidOperationException("Cannot get method name")
        };

        return methodName.ToString() switch
        {
            nameof(object.Equals) => ProvideForEqualsCall(expression),
            nameof(object.ReferenceEquals) => ProvideForReferenceEqualsCall(expression),
            nameof(Enumerable.SequenceEqual) => ProvideForSequenceEqualCall(expression),
            nameof(Enumerable.Any) => ProvideForAnyCall(expression),
            nameof(string.StartsWith) => ProvideForStartsOrEndsWithCall(expression, "starts with"),
            nameof(string.EndsWith) => ProvideForStartsOrEndsWithCall(expression, "ends with"),
            nameof(string.Contains) => ProvideForContainsCall(expression),
            _ => ProvideDefault(expression),
        };
    }

    private static ExpressionSyntax Provide(IsPatternExpressionSyntax condition)
    {
        return new InterpolatedStringBuilder()
            .AppendSyntaxAsText(condition.Expression)
            .AppendText("; Expected: ")
            .AppendSyntaxAsText(condition.Pattern)
            .AppendText("; But was: ")
            .AppendExpression(condition.Expression);
    }

    private static ExpressionSyntax Provide(CastExpressionSyntax condition)
    {
        return condition.Type is PredefinedTypeSyntax predefinedTypeSyntax
            && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.BoolKeyword)
            ? new InterpolatedStringBuilder()
                .AppendText("Expected: ")
                .AppendSyntaxAsText(condition.Expression)
            : ProvideDefault(condition);
    }

    private static ExpressionSyntax ProvideDefault(ExpressionSyntax condition)
    {
        return new InterpolatedStringBuilder()
            .AppendText("Expected: ")
            .AppendSyntaxAsText(condition);
    }
}