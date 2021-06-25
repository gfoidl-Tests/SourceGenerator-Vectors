using System;
using BenchmarkDotNet.Attributes;

#if !DEBUG
using BenchmarkDotNet.Running;
#endif

Bench bench = new();
Console.WriteLine(bench.Default());
Console.WriteLine(bench.BitArrayBased());
Console.WriteLine(bench.Vectorized());

#if !DEBUG
BenchmarkRunner.Run<Bench>();
#endif

[ShortRunJob]
[DisassemblyDiagnoser]
public class Bench
{
    //[Params("abc", "0123456789abcd❤efghij", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa❤aaaaaa")]
    public string Host { get; set; } = "0123456789abcdefghijk❤";
    //public string Host { get; set; } = "012";

    public Bench()
    {
        HttpCharacters.Initialize();
        HttpCharacters_BitArray.Initialize();
    }

    [Benchmark(Baseline = true)]
    public int Default() => HttpCharacters.IndexOfInvalidHostChar(this.Host);

    //[Benchmark]
    public int BitArrayBased() => HttpCharacters_BitArray.IndexOfInvalidHostChar(this.Host);

    [Benchmark]
    public int Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidHostChar(this.Host);
}
