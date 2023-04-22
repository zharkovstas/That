namespace That.Tests;

public partial class AssertTests
{
    public void Throws_GivenNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Assert.Throws<Exception>(null!));
    }

    public void Throws_GivenActionThatDoesNotThrow_ThrowsAssertionException()
    {
        var exception = Assert.Throws<AssertionException>(
            () => Assert.Throws<Exception>(() => { })
        );

        Assert.That(
            exception.Message == "Expected: Throws a System.Exception; But was: No exception",
            $"exception.Message; Expected: {"Expected: Throws a System.Exception; But was: No exception"}; But was: {exception.Message}");
    }

    public void Throws_GivenActionThatThrowsExpectedException_DoesNotThrow()
    {
        Assert.Throws<InvalidOperationException>(() => throw new InvalidOperationException());
    }

    public void Throws_GivenActionThatThrowsSubtypeOfExpectedException_DoesNotThrow()
    {
        Assert.Throws<Exception>(() => throw new InvalidOperationException());
    }

    public void Throws_GivenActionThatThrowsUnexpectedException_ThrowsAssertionException()
    {
        var exception = Assert.Throws<AssertionException>(
            () => Assert.Throws<InvalidOperationException>(() => throw new ArgumentNullException())
        );

        Assert.That(
            exception.Message.StartsWith(
                "Expected: Throws a System.InvalidOperationException; But was: System.ArgumentNullException",
                StringComparison.Ordinal),
            $"exception.Message; Expected: starts with {"Expected: Throws a System.InvalidOperationException; But was: System.ArgumentNullException"}; But was: {exception.Message}");
    }
}