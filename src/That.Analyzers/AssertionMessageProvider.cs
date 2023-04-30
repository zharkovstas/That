using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace That.Analyzers;

internal static partial class AssertionMessageProvider
{
    internal static ExpressionSyntax Provide(ExpressionSyntax condition)
    {
        return Provide(condition, negate: false);
    }

    private static ExpressionSyntax Provide(ExpressionSyntax condition, bool negate)
    {
        return condition switch
        {
            BinaryExpressionSyntax binaryExpression => Provide(binaryExpression, negate),
            PrefixUnaryExpressionSyntax prefixUnaryExpression => Provide(prefixUnaryExpression, negate),
            InvocationExpressionSyntax invocationExpression => Provide(invocationExpression, negate),
            CastExpressionSyntax castExpression => Provide(castExpression, negate),
            IsPatternExpressionSyntax isPatternExpression => Provide(isPatternExpression, negate),
            ParenthesizedExpressionSyntax parenthesizedExpression => Provide(parenthesizedExpression.Expression, negate),
            _ => ProvideDefault(condition, negate),
        };
    }

    private static ExpressionSyntax Provide(BinaryExpressionSyntax condition, bool negate)
    {
        var builder = new InterpolatedStringBuilder()
            .AppendSyntaxAsText(condition.Left)
            .AppendText("; Expected: ");

        return (SyntaxKind)condition.RawKind switch
        {
            SyntaxKind.EqualsExpression => builder
                .AppendText(negate ? "not " : "")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.NotEqualsExpression => builder
                .AppendText(negate ? "" : "not ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.LessThanExpression => builder
                .AppendText(negate ? ">= " : "< ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.LessThanOrEqualExpression => builder
                .AppendText(negate ? "> " : "<= ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.GreaterThanExpression => builder
                .AppendText(negate ? "<= " : "> ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            SyntaxKind.GreaterThanOrEqualExpression => builder
                .AppendText(negate ? "< " : ">= ")
                .AppendExpression(condition.Right)
                .AppendText("; But was: ")
                .AppendExpression(condition.Left),
            _ => ProvideDefault(condition, negate),
        };
    }

    private static ExpressionSyntax Provide(PrefixUnaryExpressionSyntax expression, bool negate)
    {
        return (SyntaxKind)expression.RawKind switch
        {
            SyntaxKind.LogicalNotExpression => Provide(expression.Operand, !negate),
            _ => ProvideDefault(expression, negate),
        };
    }

    private static ExpressionSyntax Provide(InvocationExpressionSyntax expression, bool negate)
    {
        if (expression.Expression is not IdentifierNameSyntax and not MemberAccessExpressionSyntax)
        {
            return ProvideDefault(expression, negate);
        }

        var methodName = expression.Expression switch
        {
            IdentifierNameSyntax identifier => identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            _ => throw new InvalidOperationException("Cannot get method name")
        };

        return methodName.ToString() switch
        {
            nameof(object.Equals) => ProvideForEqualsCall(expression, negate),
            nameof(object.ReferenceEquals) => ProvideForReferenceEqualsCall(expression, negate),
            nameof(Enumerable.SequenceEqual) => ProvideForSequenceEqualCall(expression, negate),
            nameof(Enumerable.Any) => ProvideForAnyCall(expression, negate),
            nameof(Enumerable.All) => ProvideForAllCall(expression, negate),
            nameof(string.StartsWith) => ProvideForStartsOrEndsWithCall(
                expression,
                negate ? "does not start with" : "starts with",
                negate),
            nameof(string.EndsWith) => ProvideForStartsOrEndsWithCall(
                expression,
                negate ? "does not end with" : "ends with",
                negate),
            nameof(string.Contains) => ProvideForContainsCall(expression, negate),
            nameof(double.IsNaN) => ProvideForFloatingPointNumberStaticPredicateCall(
                expression,
                negate ? "not a NaN" : "NaN",
                negate),
            nameof(double.IsInfinity) => ProvideForFloatingPointNumberStaticPredicateCall(
                expression,
                negate ? "not an infinity" : "infinity",
                negate),
            nameof(double.IsNegativeInfinity) => ProvideForFloatingPointNumberStaticPredicateCall(
                expression,
                negate ? "not the negative infinity" : "negative infinity",
                negate),
            nameof(double.IsPositiveInfinity) => ProvideForFloatingPointNumberStaticPredicateCall(
                expression,
                negate ? "not the positive infinity" : "positive infinity",
                negate),
            _ => ProvideDefault(expression, negate),
        };
    }

    private static ExpressionSyntax Provide(IsPatternExpressionSyntax condition, bool negate)
    {
        var builder = new InterpolatedStringBuilder()
                .AppendSyntaxAsText(condition.Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendSyntaxAsText(condition.Pattern)
                .AppendText("; But was: ");

        if (!negate && condition.Pattern is UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } })
        {
            return builder.AppendText("null");
        }

        if (negate && condition.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } })
        {
            return builder.AppendText("null");
        }

        return builder.AppendExpression(condition.Expression);
    }

    private static ExpressionSyntax Provide(CastExpressionSyntax condition, bool negate)
    {
        return condition.Type is PredefinedTypeSyntax predefinedTypeSyntax
            && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.BoolKeyword)
            ? new InterpolatedStringBuilder()
                .AppendText("Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendSyntaxAsText(condition.Expression)
            : ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideDefault(ExpressionSyntax condition, bool negate)
    {
        var builder = new InterpolatedStringBuilder()
            .AppendText("Expected: ")
            .AppendSyntaxAsText(condition);

        if (negate)
        {
            builder.AppendText(" to be false");
        }

        return builder;
    }
}