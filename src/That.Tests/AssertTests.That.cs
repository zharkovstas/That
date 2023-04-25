namespace That.Tests;

public partial class AssertTests
{
    public void That_Boolean_GivenTrue_DoesNotThrow()
    {
#pragma warning disable That0001
        Assert.That(true);
#pragma warning restore That0001
    }

    public void That_Boolean_GivenFalse_ThrowsAssertionException()
    {
        var exception = Assert.Throws<AssertionException>(
#pragma warning disable That0001
            () => Assert.That(false)
#pragma warning restore That0001
        );

        Assert.That(
            exception.Message == "Expectation failed",
            $"exception.Message; Expected: Expectation failed; But was: {exception.Message}");
    }

    public void That_Boolean_String_GivenTrue_DoesNotThrow()
    {
        Assert.That(true, "Earth is round");
    }

    public void That_Boolean_String_GivenFalse_ThrowsAssertionException()
    {
        var exception = Assert.Throws<AssertionException>(
            () => Assert.That(false, "Earth is flat")
        );

        Assert.That(
            exception.Message == "Earth is flat",
            $"exception.Message; Expected: Earth is flat; But was: {exception.Message}");
    }
}