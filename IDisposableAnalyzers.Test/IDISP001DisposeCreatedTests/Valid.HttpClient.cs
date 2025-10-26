namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class Valid_HttpClient
{
    private static readonly LocalDeclarationAnalyzer Analyzer = new();

    [Test]
    public static void HttpClientCreatedByConstructor_NotDisposed_NoWarning()
    {
        var code = @"
namespace N
{
    using System.Net.Http;

    public class C
    {
        public void M()
        {
            var client = new HttpClient();
            _ = client.BaseAddress;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void HttpClientCreatedByFactory_NotDisposed_NoWarning()
    {
        var iHttpClientFactory = @"
namespace System.Net.Http
{
    public interface IHttpClientFactory
    {
        HttpClient CreateClient(string name);
    }
}";

        var code = @"
namespace N
{
    using System.Net.Http;

    public class C
    {
        private readonly IHttpClientFactory _factory;

        public C(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public void M()
        {
            var client = _factory.CreateClient(""default"");
            _ = client.BaseAddress;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, iHttpClientFactory, code);
    }
}
