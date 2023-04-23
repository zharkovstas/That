using Microsoft.CodeAnalysis.CSharp;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace That.Analyzers.Tests;

public class InterpolatedStringBuilderTests
{
    public void GivenNothing_BuildsEmptyStringLiteral()
    {
        var actual = new InterpolatedStringBuilder().Build().ToString();

        Assert.That(actual == "\"\"", $"actual; Expected: \"\"; But was: {actual}");
    }

    public void GivenSingleText_BuildsStringLiteral()
    {
        var actual = new InterpolatedStringBuilder().AppendText("test").Build().ToString();

        Assert.That(actual == "\"test\"", $"actual; Expected: \"test\"; But was: {actual}");
    }

    public void GivenMultipleTexts_ConcatenatesThem()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendText("1")
            .AppendText("2")
            .AppendText("3")
            .Build()
            .ToString();

        Assert.That(actual == "\"123\"", $"actual; Expected: \"123\"; But was: {actual}");
    }

    public void GivenSyntaxAsText_EscapesIt()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendSyntaxAsText(LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal("test")))
            .Build()
            .ToString();

        Assert.That(actual == "\"\\\"test\\\"\"", $"actual; Expected: \"\\\"test\\\"\"; But was: {actual}");
    }

    public void GivenExpression_BuildsInterpolatedString()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(IdentifierName("test"))
            .Build()
            .ToString();

        Assert.That(actual == "$\"{test}\"", $"actual; Expected: {"$\"{test}\""}; But was: {actual}");
    }

    public void GivenStringLiteralExpression_BuildsStringLiteral()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendExpression(LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal("test")))
            .Build()
            .ToString();

        Assert.That(actual == "\"test\"", $"actual; Expected: \"test\"; But was: {actual}");
    }

    public void GivenTextAndExpression_EscapesText()
    {
        var actual = new InterpolatedStringBuilder()
            .AppendText("\"{}\\")
            .AppendExpression(IdentifierName("test"))
            .Build()
            .ToString();

        Assert.That(actual == "$\"\\\"{{}}\\\\{test}\"", $"actual; Expected: {"$\"\\\"{{}}\\\\{test}\""}; But was: {actual}");
    }
}