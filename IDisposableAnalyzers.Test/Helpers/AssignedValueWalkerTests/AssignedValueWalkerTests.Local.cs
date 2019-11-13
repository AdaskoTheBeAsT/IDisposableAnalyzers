﻿namespace IDisposableAnalyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class AssignedValueWalkerTests
    {
        public static class Local
        {
            [TestCase("1")]
            [TestCase("1 + 1")]
            [TestCase("Value")]
            [TestCase("abc")]
            [TestCase("default(int)")]
            [TestCase("typeof(int)")]
            [TestCase("nameof(int)")]
            [TestCase("new int[] { 1 , 2 , 3 }")]
            [TestCase("new int[2]")]
            public static void InitializedWithConstant(string expression)
            {
                var code = @"
namespace N
{
    internal class C
    {
        private const int Value = 2;

        internal C()
        {
            var value = 1;
            var temp = value;
        }
    }
}".AssertReplace("1", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = value;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual(expression, assignedValues.Single().ToString());
                }
            }

            [Test]
            public static void InitializedWithDefaultGeneric()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C<T>
    {
        internal C()
        {
            var value = default(T);
            var temp = value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = value;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("default(T)", actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public static void NotInitialized(string statement, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
            int value;
            var temp1 = value;
            value = 1;
            var temp2 = value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(statement).Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public static void NotInitializedInLambda(string statement, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                int value;
                var temp1 = value;
                value = 1;
                var temp2 = value;
            };
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(statement).Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value;", "1, 2")]
            [TestCase("var temp2 = value;", "1, 2")]
            public static void LambdaClosure(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            int value = 1;
            Console.CancelKeyPress += (o, e) =>
            {
                var temp1 = value;
                value = 2;
                var temp2 = value;
            };
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value;", "1, 2")]
            [TestCase("var temp2 = value;", "1, 2")]
            [TestCase("var temp3 = value;", "1, 2")]
            public static void Loop(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public C(int i)
        {
            int value = 1;
            while (i > 0)
            {
                var temp1 = value;
                value = 2;
                var temp2 = value;
                i--;
            }

            var temp3 = value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual(expected, actual);
                }
            }

            [Test]
            public static void AssignedWithArg()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C(int meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public static void VerbatimIdentifierAssignedWithArg()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C(int meh)
        {
            var @operator = meh;
            var value = @operator;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = @operator").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual("meh", assignedValues.Single().ToString());
                }
            }

            [Test]
            public static void AssignedWithArgGenericMethod()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal void M<T>(T meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public static void AssignedWithArgGenericClass()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C<T>
    {
        internal C(T meh)
        {
            var temp = meh;
            var value = temp;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var value = temp").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("meh", actual);
                }
            }

            [Test]
            public static void AssignedInLock()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly object gate;

        public IDisposable disposable;
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            var toDispose = (IDisposable)null;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                toDispose = this.disposable;
                this.disposable = null;
            }

            var temp = toDispose;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause("var temp = toDispose;").Value;
                using (var assignedValues = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", assignedValues);
                    Assert.AreEqual("(IDisposable)null, this.disposable", actual);
                }
            }
        }
    }
}
