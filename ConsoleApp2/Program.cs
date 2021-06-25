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
    private string _host = "0123456789#";

    public Bench()
    {
        HttpCharacters.Initialize();
        HttpCharacters_BitArray.Initialize();
    }

    //[Benchmark(Baseline = true)]
    public int Default() => HttpCharacters.IndexOfInvalidHostChar(_host);

    //[Benchmark]
    public int BitArrayBased() => HttpCharacters_BitArray.IndexOfInvalidHostChar(_host);

    [Benchmark]
    public int Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidHostChar(_host);
}
