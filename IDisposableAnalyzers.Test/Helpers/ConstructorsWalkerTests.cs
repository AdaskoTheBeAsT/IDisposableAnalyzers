namespace IDisposableAnalyzers.Test.Helpers;

using System;
using System.Linq;
using System.Threading;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

public static class ConstructorsWalkerTests
{
    [Test]
    public static void TwoInternalChained()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        internal C()
        {
        }

        internal C(string text)
            : this()
        {
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var type = syntaxTree.FindTypeDeclaration("C");
        using var walker = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None);
        var actual = string.Join(", ", walker.NonPrivateCtors.Select(c => c.ToString().Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0]));
        Assert.That(actual, Is.EqualTo("internal C(), internal C(string text)"));
        Assert.That(walker.ObjectCreations.Count, Is.EqualTo(0));
    }

    [Test]
    public static void InternalPrivateChained()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private C()
        {
        }

        internal C(string text)
            : this()
        {
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var type = syntaxTree.FindTypeDeclaration("C");
        using var pooled = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None);
        var actual = string.Join(", ", pooled.NonPrivateCtors.Select(c => c.ToString().Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0]));
        Assert.That(actual, Is.EqualTo("internal C(string text)"));
        Assert.That(pooled.ObjectCreations.Count, Is.EqualTo(0));
    }

    [Test]
    public static void PrivatePrivateFactory()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    internal class C
    {
        private C()
        {
        }

        internal C Create()
        {
            return new C();
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var type = syntaxTree.FindTypeDeclaration("C");
        using var walker = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None);
        var actual = string.Join(", ", walker.NonPrivateCtors.Select(c => c.ToString().Split('\r')[0]));
        Assert.That(actual, Is.EqualTo(string.Empty));
        Assert.That(string.Join(", ", walker.ObjectCreations), Is.EqualTo("new C()"));
    }
}
