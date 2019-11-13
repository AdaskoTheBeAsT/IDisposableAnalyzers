﻿namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(ArgumentAnalyzer))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    public static partial class Valid<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();
        private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP003DisposeBeforeReassigning;

        private const string DisposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        [Test]
        public static void LocalDeclaration()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalAssignedInSwitch()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public static class C
    {
        public static IDisposable M(int i)
        {
            IDisposable result;
            switch (i)
            {
                case 1:
                    result = File.OpenRead(string.Empty);
                    break;
                case 2:
                    result = File.OpenRead(string.Empty);
                    break;
                default:
                    result = null;
                    break;
            }

            return result;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalAssignedInIfElseSwitch()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public static class C
    {
        public static IDisposable M(int i)
        {
            IDisposable result;
            if (i == 0)
            {
                result = null;
            }
            else
            {
                switch (i)
                {
                    case 1:
                        result = File.OpenRead(string.Empty);
                        break;
                    case 2:
                        result = File.OpenRead(string.Empty);
                        break;
                    default:
                        result = null;
                        break;
                }
            }

            return result;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssignVariableInitializedWithNull()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void Meh()
        {
            Stream stream = null;
            stream = File.OpenRead(string.Empty);
            stream.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(stream as IDisposable)?.Dispose()")]
        [TestCase("(stream as IDisposable).Dispose()")]
        [TestCase("((IDisposable)stream).Dispose()")]
        [TestCase("((IDisposable)stream)?.Dispose()")]
        public static void NotDisposingVariableOfTypeObject(string disposeCode)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            (stream as IDisposable)?.Dispose();
            stream = File.OpenRead(string.Empty);
            (stream as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("(stream as IDisposable)?.Dispose()", disposeCode);
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningPropertyInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningPropertyInCtorInDisposableType()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningIndexerInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class C
    {
        private readonly List<int> ints = new List<int>();

        public C()
        {
            this[1] = 1;
        }

        public int this[int index]
        {
            get { return this.ints[index]; }
            set { this.ints[index] = value; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningIndexerInCtorInDisposableType()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class C : IDisposable
    {
        private readonly List<int> ints = new List<int>();

        public C()
        {
            this[1] = 1;
        }

        public int this[int index]
        {
            get { return this.ints[index]; }
            set { this.ints[index] = value; }
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningPropertyWithBackingFieldInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

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
        public static void AssigningFieldInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldSwapCached()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        private Stream current;

        public void SetCurrent(int number)
        {
            this.current = this.Cache[number];
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalSwapCached()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            var current = this.Cache[number];
            current = this.Cache[number + 1];
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LocalSwapCachedDisposableDictionary()
        {
            var disposableDictionaryCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class DisposableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly DisposableDictionary<int, Stream> Cache = new DisposableDictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            var current = this.Cache[number];
            current = this.Cache[number + 1];
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposableDictionaryCode, testCode);
        }

        [Test]
        public static void LocalSwapCachedTryGetValue()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            Stream current = this.Cache[number];
            this.Cache.TryGetValue(1, out current);
            Stream temp;
            this.Cache.TryGetValue(2, out temp);
            current = temp;
            current = this.Cache[number + 1];
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningInIfElse()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void Meh(bool b)
        {
            Stream stream;
            if (b)
            {
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }

            stream?.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssignFieldViaOutParameterInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
        {
            TryGetStream(out this.stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Stream stream;")]
        [TestCase("Stream stream = null;")]
        [TestCase("var stream = (Stream)null;")]
        public static void VariableSplitDeclarationAndAssignment(string declaration)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void Meh()
        {
            Stream stream;
            stream = File.OpenRead(string.Empty);
            stream.Dispose();
        }
    }
}".AssertReplace("Stream stream;", declaration);
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WithOptionalParameter()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class C
    {
        private IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = M(disposable);
        }

        private static IDisposable M(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return M(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ChainedCalls()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class C
    {
        private IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = M(disposable);
        }

        private static IDisposable M(IDisposable disposable)
        {
            if (disposable == null)
            {
                return M(disposable, new[] { disposable });
            }

            return disposable;
        }

        private static IDisposable M(IDisposable disposable, IDisposable[] list)
        {
            return disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ChainedCallsWithHelper()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        private IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = Helper.M(disposable);
        }
    }
}";

            var helperCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public static class Helper
    {
        public static IDisposable M(IDisposable disposable)
        {
            if (disposable == null)
            {
                return M(disposable, new[] { disposable });
            }

            return disposable;
        }

        public static IDisposable M(IDisposable disposable, IDisposable[] list)
        {
            return disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, helperCode, testCode);
        }

        [Test]
        public static void ReproIssue71()
        {
            var code = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxonomyWpf
{
    public class IndexedList<T> : IList<KeyValuePair<int, T>>
    {
        protected IList<T> decorated;

        public IndexedList(IList<T> decorated)
        {
            if(decorated == null)
                throw new ArgumentNullException(nameof(decorated));

            this.decorated = decorated;
        }

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            return decorated.Select((element, index) => new KeyValuePair<int, T>(index, element)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<int, T>>.Add(KeyValuePair<int, T> item)
        {
            Add(item.Value);
        }

        public void Add(T item)
        {
            decorated.Add(item);
        }

        public void Clear()
        {
            decorated.Clear();
        }

        bool ICollection<KeyValuePair<int, T>>.Contains(KeyValuePair<int, T> item)
        {
            return Contains(item.Value);
        }

        public bool Contains(T item)
        {
            return decorated.Contains(item);
        }

        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, T> item)
        {
            return decorated.Remove(item.Value);
        }

        public int Count => decorated.Count;
        public bool IsReadOnly => decorated.IsReadOnly;

        public int IndexOf(KeyValuePair<int, T> item)
        {
            return decorated.IndexOf(item.Value);
        }

        void IList<KeyValuePair<int, T>>.Insert(int index, KeyValuePair<int, T> item)
        {
            Insert(index, item.Value);
        }

        public void Insert(int index, T item)
        {
            decorated.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            decorated.RemoveAt(index);
        }
        public KeyValuePair<int, T> this[int index]
        {
            get { return new KeyValuePair<int, T>(index, decorated[index]); }
            set { decorated[index] = value.Value; }
        }
    }

    public class ObservableIndexedList<T> : IndexedList<T>, INotifyCollectionChanged
    {
        public ObservableIndexedList(ObservableCollection<T> decorated) : 
            base(decorated)
        {

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { ((ObservableCollection<T>)decorated).CollectionChanged += value; }
            remove { ((ObservableCollection<T>)decorated).CollectionChanged -= value; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingBackingFieldInSetter()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set 
            { 
                this.stream?.Dispose();
                this.stream = value; 
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void LazyProperty()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;
        private bool disposed;

        public IDisposable Disposable => this.disposable ?? (this.disposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.disposable?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void LazyAssigningSingleAssignmentDisposable()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly Lazy<int> lazy;
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

        public C(IObservable<object> observable)
        {
            this.lazy = new Lazy<int>(
                () =>
                {
                    disposable.Disposable = new Disposable();
                    return 1;
                });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void SeparateDeclarationAndAssignment()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            disposable = new Disposable();
            disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void SeparateDeclarationAndAssignmentInLambda()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                IDisposable disposable;
                disposable = new Disposable();
                disposable.Dispose();
            };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void SeparateDeclarationAndAssignmentInUsing()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void SingleSimpleAssignment()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void AssigningWithAssignment()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
        {
            this.DataContext = disposable = new Disposable();
        }

        public object DataContext { get; set; }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void TryWithEarlyReturn()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        private static bool TryGetStream(string fileName, out Stream stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingListContent()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private List<Stream> streams = new List<Stream>();

        public void Meh()
        {
            this.streams[0].Dispose();
            this.streams[0] = File.OpenRead(string.Empty);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingListContentUnderscore()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private List<Stream> _streams = new List<Stream>();

        public void Meh()
        {
            _streams[0].Dispose();
            _streams[0] = File.OpenRead(string.Empty);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningOutParameterInForeach()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public static bool TryGetStream(string[] fileNames, out Stream result)
        {
            foreach (var name in fileNames)
            {
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningOutParameterInFor()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public static bool TryGetStreamFor(string[] fileNames, out Stream result)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningOutParameterInWhile()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public static bool TryGetStreamWhile(string[] fileNames, out Stream result)
        {
            var i = 0;
            while (i < fileNames.Length)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }

                i++;
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposedAfterInForeach()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void DisposeAfter(string[] fileNames)
        {
            Stream stream = null;
            foreach (var name in fileNames)
            {
                stream = File.OpenRead(name);
                stream.Dispose();
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalAssignedTwoStepInLoop()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public static class C
    {
        public static void M()
        {
            for(var i = 0; i < 2;i++)
            {
                IDisposable result;
                result = File.OpenRead(string.Empty);

                result.Dispose();
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ChainedConstructorSettingToNullThenInjected()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
             : this(1)
        {
            this.disposable = disposable;
        }

        public C(int n)
        {
            this.disposable = null;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposeAssignDisposeAssignNull()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Threading;

    public sealed class C : IDisposable
    {
        private CancellationTokenSource cts;

        public void M()
        {
            this.cts?.Dispose();
            this.cts = new CancellationTokenSource();
            this.cts.Dispose();
            this.cts = null;
        }

        public void Dispose()
        {
            this.cts?.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldTryFinally()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Threading;

    public abstract class C : IDisposable
    {
        private bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        public void M()
        {
            try
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                _cancellationTokenSource?.Dispose();
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
