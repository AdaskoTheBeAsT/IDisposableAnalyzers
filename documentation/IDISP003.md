# IDISP003
## Dispose previous before re-assigning

| Topic    | Value
| :--      | :--
| Id       | IDISP003
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ArgumentAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ArgumentAnalyzer.cs)
|          | [AssignmentAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/AssignmentAnalyzer.cs)

## Description

Dispose previous before re-assigning.

## Motivation

In the following example the old file will not be disposed when setting it to a new file in the `Update` method.

```c#
public sealed class Foo : IDisposable
{
    private FileStream stream = File.OpenRead("file.txt");

    public void Update(string fileName)
    {
        this.stream = File.OpenRead(fileName);
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}
```

## How to fix violations

Dispose the old value before assigning a new value.

```c#
public sealed class Foo : IDisposable
{
    private FileStream stream = File.OpenRead("file.txt");

    public void Update(string fileName)
    {
        this.stream?.Dispose();
        this.stream = File.OpenRead(fileName);
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP003 // Dispose previous before re-assigning
Code violating the rule here
#pragma warning restore IDISP003 // Dispose previous before re-assigning
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP003 // Dispose previous before re-assigning
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP003:Dispose previous before re-assigning", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->