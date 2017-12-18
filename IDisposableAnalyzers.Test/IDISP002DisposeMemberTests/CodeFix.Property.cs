﻿namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class Property
        {
            [Test]
            public void PropertyWhenInitializedInline()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetOnlyPropertyWhenInitializedInline()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetSetPropertyOfTypeObjectWhenInitializedInline()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public object Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public object Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.Stream as IDisposable)?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetOnlyPropertyOfTypeObjectWhenInitializedInline()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public object Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public object Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.Stream as IDisposable)?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetSetPropertyWhenInitializedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; set; }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; set; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetOnlyPropertyWhenInitializedInCtorVirtualDisposeUnderscoreNames()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private bool _disposed;

        public Foo()
        {
            Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private bool _disposed;

        public Foo()
        {
            Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
                Stream?.Dispose();
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void GetOnlyPropertyWhenInitializedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<PropertyDeclarationAnalyzer, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            }
        }
    }
}