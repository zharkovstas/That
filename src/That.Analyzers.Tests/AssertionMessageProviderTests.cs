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
            ("actual == expected", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("actual == 2", "$\"actual; Expected: 2; But was: {actual}\""),
            ("2 == expected", "$\"2; Expected: {expected}; But was: 2\""),
            ("2 == 3", "\"2; Expected: 3; But was: 2\""),
            ("(2 == 3)", "\"2; Expected: 3; But was: 2\""),
            ("2 != 3", "\"2; Expected: not 3; But was: 2\""),
            ("2 < 3", "\"2; Expected: < 3; But was: 2\""),
            ("2 <= 3", "\"2; Expected: <= 3; But was: 2\""),
            ("2 > 3", "\"2; Expected: > 3; But was: 2\""),
            ("2 >= 3", "\"2; Expected: >= 3; But was: 2\""),
            ("!actual", "\"Expected: actual to be false\""),
            ("2.Equals(3)", "\"2; Expected: 3; But was: 2\""),
            ("Equals(2, 3)", "\"2; Expected: 3; But was: 2\""),
            ("string.Equals(actual, expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("object.Equals(2, 3)", "\"2; Expected: 3; But was: 2\""),
            ("ReferenceEquals(2, 3)", "\"2; Expected: same as 3; But was: 2\""),
            ("object.ReferenceEquals(2, 3)", "\"2; Expected: same as 3; But was: 2\""),
            ("actual.SequenceEqual(expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
            ("Enumerable.SequenceEqual(actual, expected)", "$\"actual; Expected: {expected}; But was: {actual}\""),
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