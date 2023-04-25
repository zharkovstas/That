namespace That;

/// <summary>
/// Provides the assertions
/// </summary>
public static class Assert
{
    /// <summary>
    /// Checks that <paramref name="condition"/> is true
    /// </summary>
    /// <param name="condition">the condition to check</param>
    /// <exception cref="AssertionException">
    /// Thrown when <paramref name="condition"/> is false
    /// </exception>
    public static void That(bool condition)
    {
        if (!condition)
        {
            throw new AssertionException($"Expectation failed");
        }
    }

    /// <summary>
    /// Checks that <paramref name="condition"/> is true
    /// </summary>
    /// <param name="condition">the condition to check</param>
    /// <param name="message">the message to display when <paramref name="condition"/> is false</param>
    /// <exception cref="AssertionException">
    /// Thrown when <paramref name="condition"/> is false
    /// </exception>
    public static void That(bool condition, string message)
    {
        if (!condition)
        {
            throw new AssertionException(message);
        }
    }

    /// <summary>
    /// Checks that <paramref name="action"/> throws an exception of type <typeparamref name="TException"/> when called
    /// </summary>
    /// <typeparam name="TException">the type of the expected exception</typeparam>
    /// <param name="action">the action to call</param>
    /// <returns>The thrown exception</returns>
    /// <exception cref="AssertionException">
    /// Thrown when <paramref name="action"/> does not throw an exception of the expected type
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="action"/> is null
    /// </exception>
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