using Microsoft.CodeAnalysis.CSharp;

namespace That.Analyzers.Tests;

public class AssertionMessageProviderTests
{
    public void Provide_ReturnsMessageExpression()
    {
        var cases = new[]
        {
            ("true", "\"Expected: true\""),
            ("false", "\"Expected: false\""),
            ("actual", "\"Expected: actual\""),
            (" actual ", "\"Expected: actual\""),
            ("actual/* comment */", "\"Expected: actual\""),
            ("(bool)actual", "\"Expected: actual\""),
            ("some.result.actual.val == expected", "$\"some.result.actual.val; Expected: {expected}; But was: {some.result.actual.val}\""),
            ("actual == 2", "$\"actual; Expected: 2; But was: {actual}\""),
            ("2 == expected", "$\"2; Expected: {expected}; But was: 2\""),
            ("2 == 3", "\"2; Expected: 3; But was: 2\""),
            ("(2 == 3)", "\"2; Expected: 3; But was: 2\""),
            ("!(2 == 3)", "\"2; Expected: not 3; But was: 2\""),
            ("2 != 3", "\"2; Expected: not 3; But was: 2\""),
            ("2 < 3", "\"2; Expected: < 3; But was: 2\""),
            ("2 <= 3", "\"2; Expected: <= 3; But was: 2\""),
            ("2 > 3", "\"2; Expected: > 3; But was: 2\""),
            ("2 >= 3", "\"2; Expected: >= 3; But was: 2\""),
            ("!actual", "\"Expected: actual to be false\""),
            ("!!actual", "\"Expected: actual\""),
            ("2.Equals(3)", "\"2; Expected: 3; But was: 2\""),
            ("Equals(2, 3)", "\"2; Expected: 3; But was: 2\""),
            ("string.Equals(actual, expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("string.Equals(actual, expected, StringComparison.Ordinal)", "$\"actual; Expected: {expected} (StringComparison.Ordinal); But was: {actual}\""),
            ("actual.Equals(expected, StringComparison.Ordinal)", "$\"actual; Expected: {expected} (StringComparison.Ordinal); But was: {actual}\""),
            ("string.Equals(actual, StringComparison.Ordinal)", "\"Expected: string.Equals(actual, StringComparison.Ordinal)\""),
            ("object.Equals(2, 3)", "\"2; Expected: 3; But was: 2\""),
            ("object.Equals(actualComparison, StringComparison.Ordinal)", "$\"actualComparison; Expected: {StringComparison.Ordinal}; But was: {actualComparison}\""),
            ("ReferenceEquals(2, 3)", "\"2; Expected: same as 3; But was: 2\""),
            ("object.ReferenceEquals(2, 3)", "\"2; Expected: same as 3; But was: 2\""),
            ("2.ReferenceEquals(3)", "\"2; Expected: same as 3; But was: 2\""),
            ("actual.SequenceEqual(expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("Enumerable.SequenceEqual(actual, expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("actual.StartsWith(expected)", "$\"actual; Expected: starts with {expected}; But was: {actual}\""),
            ("actual.StartsWith(expected, StringComparison.Ordinal)", "$\"actual; Expected: starts with {expected} (StringComparison.Ordinal); But was: {actual}\""),
            ("actual.StartsWith(expected, true, CultureInfo.CurrentCulture)", "$\"actual; Expected: starts with {expected} (ignore case: true, CultureInfo.CurrentCulture); But was: {actual}\""),
            ("actual.EndsWith(expected)", "$\"actual; Expected: ends with {expected}; But was: {actual}\""),
            ("actual.EndsWith(expected, StringComparison.Ordinal)", "$\"actual; Expected: ends with {expected} (StringComparison.Ordinal); But was: {actual}\""),
            ("actual.EndsWith(expected, true, CultureInfo.CurrentCulture)", "$\"actual; Expected: ends with {expected} (ignore case: true, CultureInfo.CurrentCulture); But was: {actual}\""),
            ("actual.Contains(expected)", "$\"actual; Expected: contains {expected}; But was: {actual}\""),
            ("actual.Any()", "\"actual; Expected: not <empty>; But was: <empty>\""),
            ("Enumerable.Any(actual)", "\"actual; Expected: not <empty>; But was: <empty>\""),
            ("!actual.Any()", "\"actual; Expected: <empty>; But was: not <empty>\""),
            ("!Enumerable.Any(actual)", "\"actual; Expected: <empty>; But was: not <empty>\""),
            ("actual.Any(IsEven)", "\"actual; Expected: at least one item satisfies IsEven; But was: none satisfy\""),
            ("Enumerable.Any(actual, IsEven)", "\"actual; Expected: at least one item satisfies IsEven; But was: none satisfy\""),
            ("!actual.Any(x => x > 0)", "\"actual; Expected: all items do not satisfy x => x > 0; But was: some items satisfy\""),
            ("!Enumerable.Any(actual, IsEven)", "\"actual; Expected: all items do not satisfy IsEven; But was: some items satisfy\""),
            ("actual.All(IsEven)", "\"actual; Expected: all items satisfy IsEven; But was: some items do not satisfy\""),
            ("Enumerable.All(actual, IsEven)", "\"actual; Expected: all items satisfy IsEven; But was: some items do not satisfy\""),
            ("!actual.All(x => x > 0)", "\"actual; Expected: some items do not satisfy x => x > 0; But was: all items satisfy\""),
            ("!Enumerable.All(actual, IsEven)", "\"actual; Expected: some items do not satisfy IsEven; But was: all items satisfy\""),
        };

        foreach (var (condition, expectedMessage) in cases)
        {
            var actualMessage = AssertionMessageProvider.Provide(SyntaxFactory.ParseExpression(condition));
            Assert.That(
                actualMessage.ToString() == expectedMessage,
                $"message for {condition}; Expected: {expectedMessage}; But was: {actualMessage}");
        }
    }
}