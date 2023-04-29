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
            $"exception.Message; Expected: Expected: Throws a System.Exception; But was: No exception; But was: {exception.Message}");
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
            $"exception.Message; Expected: starts with Expected: Throws a System.InvalidOperationException; But was: System.ArgumentNullException; But was: {exception.Message}");
    }

    public async Task ThrowsAsync_GivenNull_ThrowsArgumentNullExceptionAsync()
    {
        await Assert
            .ThrowsAsync<ArgumentNullException>(() => Assert.ThrowsAsync<Exception>(null!))
            .ConfigureAwait(false);
    }

    public async Task ThrowsAsync_GivenActionThatDoesNotThrow_ThrowsAssertionExceptionAsync()
    {
        var exception = await Assert
            .ThrowsAsync<AssertionException>(
                () => Assert.ThrowsAsync<Exception>(() => Task.CompletedTask)
            )
            .ConfigureAwait(false);

        Assert.That(
            exception.Message == "Expected: Throws a System.Exception; But was: No exception",
            $"exception.Message; Expected: Expected: Throws a System.Exception; But was: No exception; But was: {exception.Message}");
    }

    public async Task ThrowsAsync_GivenActionThatThrowsExpectedException_DoesNotThrow()
    {
        await Assert
            .ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Task.Delay(0).ConfigureAwait(false);
                throw new InvalidOperationException();
            })
            .ConfigureAwait(false);
    }

    public async Task ThrowsAsync_GivenActionThatThrowsSubtypeOfExpectedException_DoesNotThrow()
    {
        await Assert
            .ThrowsAsync<Exception>(async () =>
            {
                await Task.Delay(0).ConfigureAwait(false);
                throw new InvalidOperationException();
            })
            .ConfigureAwait(false);
    }

    public async Task ThrowsAsync_GivenActionThatThrowsUnexpectedException_ThrowsAssertionException()
    {
        var exception = await Assert
            .ThrowsAsync<AssertionException>(
                () => Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await Task.Delay(0).ConfigureAwait(false);
                    throw new ArgumentNullException();
                })
            )
            .ConfigureAwait(false);

        Assert.That(
            exception.Message.StartsWith(
                "Expected: Throws a System.InvalidOperationException; But was: System.ArgumentNullException",
                StringComparison.Ordinal),
            $"exception.Message; Expected: starts with Expected: Throws a System.InvalidOperationException; But was: System.ArgumentNullException; But was: {exception.Message}");
    }
}