// ReSharper disable All
namespace ValidCode.ReactiveIUi;

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

sealed class DisposeWith : IDisposable
{
    private readonly CompositeDisposable compositeDisposable = new();
    private readonly IDisposable disposable;

    public DisposeWith()
    {
        this.disposable = File.OpenRead(string.Empty).DisposeWith(this.compositeDisposable);
    }

    public void Dispose()
    {
        this.compositeDisposable.Dispose();
    }
}
