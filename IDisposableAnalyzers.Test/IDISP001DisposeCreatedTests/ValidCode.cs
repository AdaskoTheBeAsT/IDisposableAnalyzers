#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(LocalDeclarationAnalyzer))]
    [TestFixture(typeof(ArgumentAnalyzer))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    public partial class ValidCode<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly DiagnosticAnalyzer Analyzer = new T();

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string meh)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

        [TestCase("1")]
        [TestCase("new string(' ', 1)")]
        [TestCase("typeof(IDisposable)")]
        [TestCase("(IDisposable)null")]
        [TestCase("await Task.FromResult(1)")]
        [TestCase("await Task.Run(() => 1)")]
        [TestCase("await Task.Run(() => new object())")]
        [TestCase("await Task.Run(() => Type.GetType(string.Empty))")]
        [TestCase("await Task.Run(() => this.GetType())")]
        public void LanguageConstructs(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class Foo
    {
        internal async void Bar()
        {
            var value = new string(' ', 1);
        }
    }
}".AssertReplace("new string(' ', 1)", code);
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void WhenDisposingVariable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Meh()
        {
            var item = new Disposable();
            item.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void UsingFileStream()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return stream.Length;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewDisposable()
        {
            var disposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var meh = new Disposable())
            {
                return 1;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, disposableCode);
        }

        [Test]
        public void Awaiting()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class Foo
    {
        internal static async Task Bar()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
            }
        }

        internal static async Task<Stream> ReadAsync(string file)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(stream)
                                ;
            }

            stream.Position = 0;
            return stream;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AwaitingMethodReturningString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class Foo
    {
        internal static async Task Bar()
        {
            var text = await ReadAsync(string.Empty);
        }

        internal static async Task<string> ReadAsync(string text)
        {
            return text;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AwaitDownloadDataTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public async Task Bar()
        {
            using (var client = new WebClient())
            {
                var bytes = await client.DownloadDataTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FactoryMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposal : IDisposable
    {
        private Stream stream;

        public Disposal() :
            this(File.OpenRead(string.Empty))
        {
        }

        private Disposal(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposal CreateNew()
        {
            Stream stream = File.OpenRead(string.Empty);
            return new Disposal(stream);
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedDbConnectionCreateCommand()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Data.Common;

    public class Foo
    {
        public static void Bar(DbConnection conn)
        {
            using(var command = conn.CreateCommand())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedMemberDbConnectionCreateCommand()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Data.Common;

    public class Foo
    {
        private readonly DbConnection connection;

        public Foo(DbConnection connection)
        {
            this.connection = connection;
        }

        public void Bar()
        {
            using(var command = this.connection.CreateCommand())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposedInEventLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class Foo
    {
        static Task RunProcessAsync(string fileName)
        {
            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
                          {
                              StartInfo = { FileName = fileName },
                              EnableRaisingEvents = true
                          };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            Stream stream;
            if (TryGetStream(out stream))
            {
                using (stream)
                {
                }
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void NewDisposableSplitDeclarationAndAssignment()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void DisposeInFinally()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class Foo
    {
        public void Bar()
        {
            FileStream currentStream = null;
            try
            {
                currentStream = File.OpenRead(string.Empty);
            }
            finally
            {
                currentStream.Dispose();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalAssignedToLocalThatIsDisposed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var stream = File.OpenRead(file);
            var temp = stream;
            temp.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file), new object())")]
        [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
        [TestCase("new Tuple<FileStream, object>(File.OpenRead(file), new object())")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
        public void LocalTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = Tuple.Create(File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file), new object())", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file), new object())")]
        [TestCase("(File.OpenRead(file), File.OpenRead(file))")]
        public void LocalValueTupleThatDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = (File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file), new object())", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FieldValueTuple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class ValueTupleOfFileStreams : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public ValueTupleOfFileStreams(string file)
        {
            this.tuple = (File.OpenRead(file), File.OpenRead(file));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void LocalPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace RoslynSandbox
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file1, string file2)
        {
            var pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
            pair.Item1.Dispose();
            pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, genericPairCode, staticPairCode, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldValueTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace RoslynSandbox
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, genericPairCode, staticPairCode, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldTuple(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public void FieldValueTuple(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public void Pair(string expression)
        {
            var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var genericPairCode = @"
namespace RoslynSandbox
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            AnalyzerAssert.Valid(Analyzer, genericPairCode, staticPairCode, testCode);
        }
    }
}
