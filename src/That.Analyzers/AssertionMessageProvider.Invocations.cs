using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace That.Analyzers;

internal static partial class AssertionMessageProvider
{
    private static ExpressionSyntax ProvideForEqualsCall(InvocationExpressionSyntax condition, bool negate)
    {
        var arguments = condition.ArgumentList.Arguments;

        // Equals(actual, expected)
        if (condition.Expression is IdentifierNameSyntax && arguments.Count == 2 && !IsStringComparison(arguments[1].Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[1].Expression)
                .AppendText("; But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        // actual.Equals(expected)
        if (arguments.Count == 1 && !IsStringType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[0].Expression)
                .AppendText("; But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // actual.Equals(expected, StringComparison.*)
        if (arguments.Count == 2
            && !IsStringType(memberAccessExpression.Expression)
            && !IsObjectType(memberAccessExpression.Expression)
            && IsStringComparison(arguments[1].Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[0].Expression)
                .AppendText(" (")
                .AppendSyntaxAsText(arguments[1].Expression)
                .AppendText("); But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // object.Equals(actual, expected)
        // string.Equals(actual, expected)
        if (arguments.Count == 2
            && (IsStringType(memberAccessExpression.Expression) && !IsStringComparison(arguments[1].Expression)
                || IsObjectType(memberAccessExpression.Expression)))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[1].Expression)
                .AppendText("; But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        // string.Equals(actual, expected, StringComparison.*)
        if (arguments.Count == 3 && IsStringType(memberAccessExpression.Expression) && IsStringComparison(arguments[2].Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[1].Expression)
                .AppendText(" (")
                .AppendSyntaxAsText(arguments[2].Expression)
                .AppendText("); But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForReferenceEqualsCall(InvocationExpressionSyntax condition, bool negate)
    {
        var arguments = condition.ArgumentList.Arguments;

        // ReferenceEquals(actual, expected)
        if (condition.Expression is IdentifierNameSyntax && arguments.Count == 2)
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate ? "; Expected: not same as " : "; Expected: same as ")
                .AppendExpression(arguments[1].Expression)
                .AppendText("; But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        // actual.ReferenceEquals(expected)
        if (arguments.Count == 1 && !IsObjectType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText(negate ? "; Expected: not same as " : "; Expected: same as ")
                .AppendExpression(arguments[0].Expression)
                .AppendText("; But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // object.ReferenceEquals(actual, expected)
        if (arguments.Count == 2 && IsObjectType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate ? "; Expected: not same as " : "; Expected: same as ")
                .AppendExpression(arguments[1].Expression)
                .AppendText("; But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForSequenceEqualCall(InvocationExpressionSyntax condition, bool negate)
    {
        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        var arguments = condition.ArgumentList.Arguments;

        // actual.SequenceEqual(expected)
        if (arguments.Count == 1 && !IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[0].Expression)
                .AppendText("; But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // Enumerable.SequenceEqual(actual, expected)
        if (arguments.Count == 2 && IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText("; Expected: ")
                .AppendText(negate ? "not " : "")
                .AppendExpression(arguments[1].Expression)
                .AppendText("; But was: ")
                .AppendExpression(arguments[0].Expression);
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForStartsOrEndsWithCall(InvocationExpressionSyntax condition, string predicate, bool negate)
    {
        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        var arguments = condition.ArgumentList.Arguments;
        if (arguments.Count == 0 || arguments.Count > 3)
        {
            return ProvideDefault(condition, negate);
        }

        var builder = new InterpolatedStringBuilder();
        builder
            .AppendSyntaxAsText(memberAccessExpression.Expression)
            .AppendText($"; Expected: {predicate} ")
            .AppendExpression(arguments[0].Expression);

        // actual.StartsWith(expected)
        // actual.EndsWith(expected)
        if (arguments.Count == 1)
        {
            return builder
                .AppendText("; But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // actual.StartsWith(expected, StringComparison.*)
        // actual.EndsWith(expected, StringComparison.*)
        if (arguments.Count == 2 && IsStringComparison(arguments[1].Expression))
        {
            return builder
                .AppendText(" (")
                .AppendSyntaxAsText(arguments[1].Expression)
                .AppendText("); But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        // actual.StartsWith(expected, ignoreCase: true, CultureInfo.*)
        // actual.EndsWith(expected, ignoreCase: false, CultureInfo.*)
        if (arguments.Count == 3)
        {
            return builder
                .AppendText(" (ignore case: ")
                .AppendExpression(arguments[1].Expression)
                .AppendText(", ")
                .AppendSyntaxAsText(arguments[2].Expression)
                .AppendText("); But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForContainsCall(InvocationExpressionSyntax condition, bool negate)
    {
        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        var arguments = condition.ArgumentList.Arguments;

        // actual.Contains(expected)
        if (arguments.Count == 1)
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText(negate ? "; Expected: does not contain " : "; Expected: contains ")
                .AppendExpression(arguments[0].Expression)
                .AppendText("; But was: ")
                .AppendExpression(memberAccessExpression.Expression);
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForAnyCall(InvocationExpressionSyntax condition, bool negate)
    {
        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        var arguments = condition.ArgumentList.Arguments;

        // actual.Any()
        if (arguments.Count == 0 && !IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText(negate
                    ? "; Expected: <empty>; But was: not <empty>"
                    : "; Expected: not <empty>; But was: <empty>");
        }

        // Enumerable.Any(actual)
        if (arguments.Count == 1 && IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate
                    ? "; Expected: <empty>; But was: not <empty>"
                    : "; Expected: not <empty>; But was: <empty>");
        }

        // actual.Any(IsCorrect)
        // actual.Any(x => x > 0)
        if (arguments.Count == 1 && !IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText(negate
                    ? "; Expected: all items do not satisfy "
                    : "; Expected: at least one item satisfies ")
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate
                    ? "; But was: some items satisfy"
                    : "; But was: none satisfy");
        }

        // Enumerable.Any(actual, IsCorrect)
        // Enumerable.Any(actual, x => x > 0)
        if (arguments.Count == 2 && IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate
                    ? "; Expected: all items do not satisfy "
                    : "; Expected: at least one item satisfies ")
                .AppendSyntaxAsText(arguments[1].Expression)
                .AppendText(negate
                    ? "; But was: some items satisfy"
                    : "; But was: none satisfy");
        }

        return ProvideDefault(condition, negate);
    }

    private static ExpressionSyntax ProvideForAllCall(InvocationExpressionSyntax condition, bool negate)
    {
        if (condition.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return ProvideDefault(condition, negate);
        }

        var arguments = condition.ArgumentList.Arguments;

        // actual.All(IsCorrect)
        // actual.All(x => x > 0)
        if (arguments.Count == 1 && !IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(memberAccessExpression.Expression)
                .AppendText(negate
                    ? "; Expected: some items do not satisfy "
                    : "; Expected: all items satisfy ")
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate
                    ? "; But was: all items satisfy"
                    : "; But was: some items do not satisfy");
        }

        // Enumerable.All(actual, IsCorrect)
        // Enumerable.All(actual, x => x > 0)
        if (arguments.Count == 2 && IsEnumerableType(memberAccessExpression.Expression))
        {
            return new InterpolatedStringBuilder()
                .AppendSyntaxAsText(arguments[0].Expression)
                .AppendText(negate
                    ? "; Expected: some items do not satisfy "
                    : "; Expected: all items satisfy ")
                .AppendSyntaxAsText(arguments[1].Expression)
                .AppendText(negate
                    ? "; But was: all items satisfy"
                    : "; But was: some items do not satisfy");
        }

        return ProvideDefault(condition, negate);
    }

    private static bool IsStringComparison(ExpressionSyntax expression)
    {
        return expression is MemberAccessExpressionSyntax memberAccessExpression
            && memberAccessExpression.Expression is IdentifierNameSyntax identifierName
            && identifierName.ToString() == nameof(StringComparison);
    }

    private static bool IsStringType(ExpressionSyntax expression)
    {
        return IsKeywordType(expression, SyntaxKind.StringKeyword);
    }

    private static bool IsObjectType(ExpressionSyntax expression)
    {
        return IsKeywordType(expression, SyntaxKind.ObjectKeyword);
    }

    private static bool IsEnumerableType(ExpressionSyntax expression)
    {
        return expression.WithoutTrivia().ToString() == nameof(Enumerable);
    }

    private static bool IsKeywordType(ExpressionSyntax expression, SyntaxKind keywordKind)
    {
        return expression is PredefinedTypeSyntax predefinedTypeSyntax
            && predefinedTypeSyntax.Keyword.IsKind(keywordKind);
    }
}