# That

`That` is a minimalistic assertion library for .NET.

## Usage

### Simple assertions

```csharp

Assert.That(actual1); // a boolean

Assert.That(false); // throws an AssertionException

Assert.That(actual2 is not null);

Assert.That(actual3 == "expected");
```

### Exceptions

```csharp
var exception = Assert.Throws<InvalidOperationException>(() => podBayDoors.Open());

Assert.That(exception.Message == "I'm sorry Dave");
```

### Descriptive fail messages

```csharp
Assert.That(actual is null, $"actual; Expected: null; But was: {actual}");
```

`That.Analyzers` provides a code-fix to auto-generate these messages based on the expression passed as the first argument to `Assert.That`.