
using BenchmarkDotNet.Running;
using ExpDataWorker.Benchmark;

//BenchmarkRunner.Run(typeof(Program).Assembly);
BenchmarkRunner.Run<CreateMemberAccessBenchmark>();
BenchmarkRunner.Run<SearchValueBenchmark>();