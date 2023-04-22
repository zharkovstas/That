namespace That;

public static class Assert
{
    public static void That(bool condition)
    {
        if (!condition)
        {
            throw new AssertionException($"Expectation failed");
        }
    }

    public static void That(bool condition, string message)
    {
        if (!condition)
        {
            throw new AssertionException(message);
        }
    }

    public static TException Throws<TException>(Action action) where TException : Exception
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        try
        {
            action();
        }
        catch (TException expectedException)
        {
            return expectedException;
        }
        catch (Exception unexpectedException)
        {
            throw new AssertionException(
                $"Expected: Throws a {typeof(TException).FullName}; But was: {unexpectedException}",
                unexpectedException);
        }

        throw new AssertionException($"Expected: Throws a {typeof(TException).FullName}; But was: No exception");
    }
}