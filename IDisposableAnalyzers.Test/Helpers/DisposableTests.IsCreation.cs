namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        internal class IsCreation
        {
            [TestCase("1",                                                   Result.No)]
            [TestCase("new string(' ', 1)",                                  Result.No)]
            [TestCase("new Disposable()",                                    Result.Yes)]
            [TestCase("new Disposable() as object",                          Result.Yes)]
            [TestCase("(object) new Disposable()",                           Result.Yes)]
            [TestCase("typeof(IDisposable)",                                 Result.No)]
            [TestCase("(IDisposable)null",                                   Result.No)]
            [TestCase("System.IO.File.OpenRead(string.Empty) ?? null",       Result.Yes)]
            [TestCase("null ?? System.IO.File.OpenRead(string.Empty)",       Result.Yes)]
            [TestCase("true ? null : System.IO.File.OpenRead(string.Empty)", Result.Yes)]
            [TestCase("true ? System.IO.File.OpenRead(string.Empty) : null", Result.Yes)]
            public void LanguageConstructs(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("new List<IDisposable>().Find(x => true)",          Result.No)]
            [TestCase("ImmutableList<IDisposable>.Empty.Find(x => true)", Result.No)]
            [TestCase("new Queue<IDisposable>().Peek()",                  Result.No)]
            [TestCase("ImmutableQueue<IDisposable>.Empty.Peek()",         Result.No)]
            [TestCase("new List<IDisposable>()[0]",                       Result.No)]
            [TestCase("Moq.Mock.Of<IDisposable>()",                       Result.AssumeNo)]
            [TestCase("ImmutableList<IDisposable>.Empty[0]",              Result.No)]
            public void Ignored(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticCreateIntStatementBody()")]
            [TestCase("StaticCreateIntExpressionBody()")]
            [TestCase("StaticCreateIntWithArg()")]
            [TestCase("StaticCreateIntId()")]
            [TestCase("StaticCreateIntSquare()")]
            [TestCase("this.CreateIntStatementBody()")]
            [TestCase("CreateIntExpressionBody()")]
            [TestCase("CreateIntWithArg()")]
            [TestCase("CreateIntId()")]
            [TestCase("CreateIntSquare()")]
            [TestCase("Id<int>()")]
            public void MethodReturningNotDisposable(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            // Meh();
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int StaticCreateIntWithArg(int arg) => 3;

        internal static int StaticCreateIntId(int arg) => arg;

        internal static int StaticCreateIntSquare(int arg) => arg * arg;

        internal int CreateIntStatementBody()
        {
            return 1;
        }

        internal int CreateIntExpressionBody() => 2;

        internal int CreateIntWithArg(int arg) => 3;
   
        internal int CreateIntId(int arg) => arg;
   
        internal int CreateIntSquare(int arg) => arg * arg;

        internal T Id<T>(T arg) => arg;
    }
}".AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(code);
                Assert.AreEqual(Result.No, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Id<IDisposable>(null)",                            Result.No)]
            [TestCase("this.Id<IDisposable>(null)",                       Result.No)]
            [TestCase("this.Id<IDisposable>(this.disposable)",            Result.No)]
            [TestCase("this.Id<IDisposable>(new Disposable())",           Result.No)]
            [TestCase("this.Id<object>(new Disposable())",                Result.No)]
            [TestCase("CreateDisposableStatementBody()",                  Result.Yes)]
            [TestCase("this.CreateDisposableStatementBody()",             Result.Yes)]
            [TestCase("CreateDisposableExpressionBody()",                 Result.Yes)]
            [TestCase("CreateDisposableExpressionBodyReturnTypeObject()", Result.Yes)]
            [TestCase("CreateDisposableInIf(true)",                       Result.Yes)]
            [TestCase("CreateDisposableInElse(true)",                     Result.Yes)]
            [TestCase("ReturningLocal()",                                 Result.Yes)]
            public void Call(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        private readonly IDisposable disposable = new Disposable();

        internal C()
        {
            // Meh();
        }

        internal T Id<T>(T arg) => arg;

        internal T ConstrainedFactory<T>(T arg) where T : IDisposable, new() => new T();

        internal T ConstrainedStructFactory<T>(T arg) where T : struct, new() => new T();

        internal IDisposable CreateDisposableStatementBody()
        {
            return new Disposable();
        }

        internal IDisposable CreateDisposableExpressionBody() => new Disposable();
       
        internal object CreateDisposableExpressionBodyReturnTypeObject() => new Disposable();

        internal IDisposable CreateDisposableInIf(bool flag)
        {
            if (flag)
            {
                return new Disposable();
            }
            else
            {
                return null;
            }

            return null;
        }

        internal IDisposable CreateDisposableInElse(bool flag)
        {
            if (flag)
            {
                return null;
            }
            else
            {
                return new Disposable();
            }

            return null;
        }

        public static Stream ReturningLocal()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}".AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(code);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("StaticRecursiveStatementBody()",  Result.No)]
            [TestCase("StaticRecursiveExpressionBody()", Result.No)]
            [TestCase("CallingRecursive()",              Result.No)]
            [TestCase("RecursiveTernary(true)",          Result.Yes)]
            [TestCase("this.RecursiveExpressionBody()",  Result.No)]
            [TestCase("this.RecursiveStatementBody()",   Result.No)]
            public void CallRecursiveMethod(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal C()
        {
            // Meh();
        }

        private static IDisposable StaticRecursiveStatementBody()
        {
            return StaticRecursiveStatementBody();
        }

        private static IDisposable StaticRecursiveExpressionBody() => StaticRecursiveExpressionBody();

        private static IDisposable CallingRecursive() => StaticRecursiveStatementBody();

        private static IDisposable RecursiveTernary(bool flag) => flag ? new Disposable() : RecursiveTernary(bool flag);

        private IDisposable RecursiveStatementBody()
        {
            return this.RecursiveStatementBody();
        }

        private IDisposable RecursiveExpressionBody() => this.RecursiveExpressionBody();
    }
}".AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation(code);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void RecursiveWithOptionalParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public abstract class C
    {
        public C(IDisposable disposable)
        {
            var value = disposable;
            value = WithOptionalParameter(value);
        }

        private static IDisposable WithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return WithOptionalParameter(value, new[] { value });
            }

            return value;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation("WithOptionalParameter(value)");
                Assert.AreEqual(Result.No, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("Task.Run(() => 1)",                         Result.No)]
            [TestCase("Task.Run(() => new Disposable())",          Result.No)]
            [TestCase("CreateStringAsync()",                       Result.No)]
            [TestCase("await CreateStringAsync()",                 Result.No)]
            [TestCase("await Task.Run(() => new string(' ', 1))",  Result.No)]
            [TestCase("await Task.FromResult(new string(' ', 1))", Result.No)]
            [TestCase("await Task.Run(() => new Disposable())",    Result.Yes)]
            [TestCase("await Task.FromResult(new Disposable())",   Result.Yes)]
            [TestCase("await CreateDisposableAsync()",             Result.Yes)]
            public void AsyncAwait(string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        internal async Task M()
        {
            var value = // Meh();
        }

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
        }

        internal static async Task<IDisposable> CreateDisposableAsync()
        {
            await Task.Delay(0);
            return new Disposable();
        }
    }
}".AssertReplace("// Meh()", code);

                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code)
                                      .Value;
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void CompositeDisposableExtAddAndReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public C()
        {
            disposable.AddAndReturn(File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindInvocation("disposable.AddAndReturn(File.OpenRead(string.Empty))");
                Assert.AreEqual(Result.No, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.AsCustom()")]
            [TestCase("disposable.AsCustom() ?? other")]
            [TestCase("other ?? disposable.AsCustom()")]
            public void AssumeYesForExtensionMethodReturningDifferentTypeThanThisParameter(string expression)
            {
                var binary = @"
namespace BinaryReference
{
    using System;

    public static class Extensions
    {
        public static ICustomDisposable AsCustom(this IDisposable disposable) => default(ICustomDisposable);
    }

    public interface ICustomDisposable : IDisposable
    {
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using BinaryReference;

    class C
    {
        public C(IDisposable disposable, ICustomDisposable other)
        {
            _ = disposable.AsCustom();
        }
    }
}".AssertReplace("disposable.AsCustom()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var references = MetadataReferences.FromAttributes()
                                                   .Concat(new[] { MetadataReferences.CreateBinary(binary) });
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, references);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(Result.AssumeYes, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.Fluent()")]
            [TestCase("disposable.Fluent() ?? other")]
            [TestCase("other ?? disposable.Fluent()")]
            public void AssumeNoForUnknownExtensionMethodReturningSameTypeAsThisParameter(string expression)
            {
                var binary = @"
namespace BinaryReference
{
    using System;

    public static class Extensions
    {
        public static IDisposable Fluent(this IDisposable disposable) => disposable;
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using BinaryReference;

    class C
    {
        public C(IDisposable disposable, IDisposable other)
        {
            _ = disposable.Fluent();
        }
    }
}".AssertReplace("disposable.Fluent()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var references = MetadataReferences.FromAttributes()
                                                   .Concat(new[] { MetadataReferences.CreateBinary(binary) });
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, references);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(Result.AssumeNo, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("",                                                                                   "System.IO.File.OpenText(string.Empty)",          Result.Yes)]
            [TestCase("",                                                                                   "System.IO.File.OpenRead(string.Empty)",          Result.Yes)]
            [TestCase("",                                                                                   "System.IO.File.ReadAllLines(string.Empty)",      Result.No)]
            [TestCase("System.IO.FileInfo info",                                                            "info.OpenRead()",                                Result.Yes)]
            [TestCase("Microsoft.Win32.RegistryKey key",                                                    "key.CreateSubKey(string.Empty)",                 Result.Yes)]
            [TestCase("System.Collections.Generic.List<int> xs",                                            "xs.GetEnumerator()",                             Result.Yes)]
            [TestCase("System.Windows.Controls.PasswordBox passwordBox",                                    "passwordBox.SecurePassword",                     Result.Yes)]
            [TestCase("System.Collections.Generic.List<int> xs",                                            "((System.Collections.IList)xs).GetEnumerator()", Result.No)]
            [TestCase("System.Collections.Generic.List<IDisposable> xs",                                    "xs.First()",                                     Result.No)]
            [TestCase("System.Collections.Generic.Dictionary<int, IDisposable> map",                        "map[0]",                                         Result.No)]
            [TestCase("System.Collections.Generic.IReadOnlyDictionary<int, IDisposable> map",               "map[0]",                                         Result.No)]
            [TestCase("System.Runtime.CompilerServices.ConditionalWeakTable<IDisposable, IDisposable> map", "map.GetOrCreateValue(this.disposable)",          Result.No)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetStream(null)",                        Result.No)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetStream(null, null)",                  Result.No)]
            [TestCase("System.Resources.ResourceManager manager",                                           "manager.GetResourceSet(null, true, true)",       Result.No)]
            [TestCase("System.Net.Http.HttpResponseMessage message",                                        "message.EnsureSuccessStatusCode()",              Result.No)]
            public void ThirdParty(string parameter, string code, Result expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq;

    internal class C
    {
        internal C(int value)
        {
            _ = value;
        }
    }
}".AssertReplace("int value", parameter)
  .AssertReplace("value", code);

                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(code);
                Assert.AreEqual(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
                Assert.IsNotInstanceOf<IErrorTypeSymbol>(type);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
