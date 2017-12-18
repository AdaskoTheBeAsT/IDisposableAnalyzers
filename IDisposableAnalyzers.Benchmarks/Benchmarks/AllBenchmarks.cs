// ReSharper disable RedundantNameQualifier
namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark DisposeMethodAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.DisposeMethodAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark FieldDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.FieldDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP001DisposeCreatedBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP001DisposeCreated());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP003DisposeBeforeReassigningBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP003DisposeBeforeReassigning());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP004DontIgnoreReturnValueOfTypeIDisposableBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP004DontIgnoreReturnValueOfTypeIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP005ReturntypeShouldIndicateIDisposableBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP005ReturntypeShouldIndicateIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP006ImplementIDisposableBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP006ImplementIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP007DontDisposeInjectedBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP007DontDisposeInjected());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP008DontMixInjectedAndCreatedForMemberBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP008DontMixInjectedAndCreatedForMember());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP009IsIDisposableBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP009IsIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark PropertyDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.PropertyDeclarationAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void DisposeMethodAnalyzer()
        {
            DisposeMethodAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void FieldDeclarationAnalyzer()
        {
            FieldDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP001DisposeCreated()
        {
            IDISP001DisposeCreatedBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP003DisposeBeforeReassigning()
        {
            IDISP003DisposeBeforeReassigningBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP004DontIgnoreReturnValueOfTypeIDisposable()
        {
            IDISP004DontIgnoreReturnValueOfTypeIDisposableBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP005ReturntypeShouldIndicateIDisposable()
        {
            IDISP005ReturntypeShouldIndicateIDisposableBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP006ImplementIDisposable()
        {
            IDISP006ImplementIDisposableBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP007DontDisposeInjected()
        {
            IDISP007DontDisposeInjectedBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP008DontMixInjectedAndCreatedForMember()
        {
            IDISP008DontMixInjectedAndCreatedForMemberBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP009IsIDisposable()
        {
            IDISP009IsIDisposableBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void PropertyDeclarationAnalyzer()
        {
            PropertyDeclarationAnalyzerBenchmark.Run();
        }
    }
}
