# IDISP023
## Don't use reference types in finalizer context.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP023</td>
  </tr>
  <tr>
    <td>Severity</td>
    <td>Warning</td>
  </tr>
  <tr>
    <td>Enabled</td>
    <td>True</td>
  </tr>
  <tr>
    <td>Category</td>
    <td>IDisposableAnalyzers.Correctness</td>
  </tr>
  <tr>
    <td>Code</td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs">DisposeMethodAnalyzer</a></td>
  </tr>
  <tr>
    <td></td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/FinalizerAnalyzer.cs">FinalizerAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Don't use reference types in finalizer context.

## Motivation

Any use of reference types from a finalizer is hazardous. When that finalizer was invoked as part of AppDomain shutdown, the CLR makes no guarantee regarding order of finalization or GC for a typical finalizer, and thus any access to a reference type (other than strictly this) could AV and crash the process. The only safe activity from a finalizer is accessing value types and calling p/invoke methods, which limits safe activity to calling into native code to release resources. Even accessing SafeHandles is unsafe, which is why these types have their own finalizers rather than relying on their owners to dispose of them during finalization.

## How to fix violations

Invalid:

```cs
protected virtual void Dispose(bool disposing)
{
   if (disposing)
   {
   }

   this.logger.Log("In Dispose(bool)"); // violation! ILogger is a ref type and we're not inside the above block
}
```

Valid:

```cs
protected virtual void Dispose(bool disposing)
{
   if (disposing)
   {
       this.logger.Log("In Dispose(bool)");
   }
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP023 // Don't use reference types in finalizer context.
Code violating the rule here
#pragma warning restore IDISP023 // Don't use reference types in finalizer context.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP023 // Don't use reference types in finalizer context.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP023:Don't use reference types in finalizer context.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->
