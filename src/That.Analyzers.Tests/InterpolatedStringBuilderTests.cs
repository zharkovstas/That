using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace That.Analyzers.Tests;

public class InterpolatedStringBuilderTests
{
    public void GivenNothing_BuildsEmptyStringLiteral()
    {
        var actual = new InterpolatedStringBuilder().Build();

        Assert.That(actual.ToString() == "\"\"", $"actual.ToString(); Expected: \"\"; But was: {actual}");
    }

    public void GivenSingleText_BuildsStringLiteral()
    {
        var actual = new InterpolatedStringBuilder().AppendText("test").Build();

        Assert.That(actual.ToString() == "\"test\"", $"actual.ToString(); Expected: \"test\"; But was: {actual}");
    }

    public void GivenMultipleTexts_ConcatenatesThem()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendText("1")
            .AppendText("2")
            .AppendText("3")
            .Build();

        Assert.That(actual.ToString() == "\"123\"", $"actual.ToString(); Expected: \"123\"; But was: {actual}");
    }

    public void GivenSyntaxAsText_EscapesIt()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendSyntaxAsText(ParseExpression("\"test\""))
            .Build();

        Assert.That(actual.ToString() == "\"\\\"test\\\"\"", $"actual.ToString(); Expected: \"\\\"test\\\"\"; But was: {actual}");
    }

    public void GivenExpression_BuildsInterpolatedString()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(IdentifierName("test"))
            .Build();

        Assert.That(actual.ToString() == "$\"{test}\"", $"actual.ToString(); Expected: $\"{{test}}\"; But was: {actual}");
    }

    public void GivenStringLiteralExpression_BuildsStringLiteral()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(ParseExpression("\"test\""))
            .Build();

        Assert.That(actual.ToString() == "\"test\"", $"actual.ToString(); Expected: \"test\"; But was: {actual}");
    }

    public void GivenTextAndExpression_EscapesText()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendText("\"{}\\")
            .AppendExpression(IdentifierName("test"))
            .Build();

        Assert.That(actual.ToString() == "$\"\\\"{{}}\\\\{test}\"", $"actual.ToString(); Expected: $\"\\\"{{{{}}}}\\\\{{test}}\"; But was: {actual}");
    }

    public void GivenToStringExpression_RemovesToString()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(ParseExpression("test.ToString()"))
            .Build();

        Assert.That(actual.ToString() == "$\"{test}\"", $"actual.ToString(); Expected: $\"{{test}}\"; But was: {actual}");
    }

    public void GivenParenthesizedExpression_RemovesParentheses()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(ParseExpression("(test)"))
            .Build();

        Assert.That(actual.ToString() == "$\"{test}\"", $"actual.ToString(); Expected: $\"{{test}}\"; But was: {actual}");
    }
}