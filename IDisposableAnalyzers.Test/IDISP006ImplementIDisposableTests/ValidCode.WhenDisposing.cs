namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class WhenDisposing
        {
            [TestCase("this.stream.Dispose();")]
            [TestCase("this.stream?.Dispose();")]
            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public void DisposingField(string disposeCall)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}".AssertReplace("this.stream.Dispose();", disposeCall);
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingFieldInVirtualDispose()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingFieldInVirtualDispose2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable _disposable = new Disposable();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }

        protected void VerifyDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void DisposingFieldInExpressionBodyDispose()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    class Goof : IDisposable {
        IDisposable _disposable;
        public void Create()  => _disposable = new Disposable();
        public void Dispose() => _disposable.Dispose();
    }
}";
                RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void DisposingFieldAsCast()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = this.stream as IDisposable;
            disposable?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingFieldInlineAsCast()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingFieldExplicitCast()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = (IDisposable)this.stream;
            disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingFieldInlineExplicitCast()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.stream).Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingPropertyWhenInitializedInProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DisposingPropertyWhenInitializedInline()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public Stream Stream { get; private set; } = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtor1()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable bar;
        
        public C(IDisposable bar)
        {
            this.bar = bar;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtor2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable bar)
        {
            _bar = bar;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtor3()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [TestCase("disposables.First();")]
            [TestCase("disposables.Single();")]
            public void IgnoreLinq(string linq)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq;

    public sealed class C
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable[] disposables)
        {
            _bar = disposables.First();
        }
    }
}".AssertReplace("disposables.First();", linq);
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoredWhenNotAssigned()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable bar;
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoredWhenBackingField()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private Stream stream;

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreFieldThatIsNotDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = new object();
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreFieldThatIsNotDisposableAssignedWithMethod1()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = Meh();

        private static object Meh() => new object();
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreFieldThatIsNotDisposableAssignedWIthMethod2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly object bar = string.Copy(string.Empty);
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoredStaticField()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        private static Stream stream = File.OpenRead(string.Empty);
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreTask()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task stream = Task.Delay(0);
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreTaskOfInt()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task<int> stream = Task.FromResult(0);
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void FieldOfTypeArrayOfInt()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private readonly int[] ints = new[] { 1, 2, 3 };
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void PropertyWithBackingFieldOfTypeArrayOfInt()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private int[] ints;

        public int[] Ints
        {
            get
            {
                return this.ints ?? (this.ints = new int[] { });
            }

            set
            {
                this.ints = value;
            }
        }

        public bool HasInts => (this.ints != null) && (this.ints.Length > 0);
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
