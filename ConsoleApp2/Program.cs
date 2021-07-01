using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BenchmarkDotNet.Attributes;

#if !DEBUG
using BenchmarkDotNet.Running;
#endif

Bench bench = new();
//bench.GlobalSetup();
//Console.WriteLine(bench.Token.Length);
//Console.WriteLine(bench.TokenBytes.Length);
//Console.WriteLine();

Console.WriteLine(bench.Default());
Console.WriteLine(bench.Vectorized());

#if !DEBUG
BenchmarkRunner.Run<Bench>();
#endif

[ShortRunJob]
[DisassemblyDiagnoser]
public class Bench
{
    //[Params("abc", "0123456789abcd❤efghij", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa❤aaaaaa")]
    public string Host { get; set; } = "0123456789abcdefghij❤k";
    //public string Host { get; set; } = "microsoft.com";

    public Bench()
    {
        HttpCharacters.Initialize();
        HttpCharacters_BitArray.Initialize();
    }

    //[Benchmark(Baseline = true)]
    public int Default() => HttpCharacters.IndexOfInvalidFieldValueCharExtended(this.Host);

    //[Benchmark]
    public int BitArrayBased() => HttpCharacters_BitArray.IndexOfInvalidHostChar(this.Host);

    [Benchmark]
    public int Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidFieldValueCharExtended(this.Host);
}

[ShortRunJob]
[DisassemblyDiagnoser]
public class BenchByte
{
    //[Params("abc", "0123456789abcd❤efghij", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa❤aaaaaa")]
    public string Token { get; set; } = "0123456789abcdef❤ghijk";
    //public string Host { get; set; } = "012";

    public byte[]? TokenBytes { get; set; }

    [GlobalSetup]
    [MemberNotNull(nameof(TokenBytes))]
    public void GlobalSetup() => this.TokenBytes = Encoding.UTF8.GetBytes(this.Token);

    public BenchByte()
    {
        HttpCharacters.Initialize();
    }

    //[Benchmark(Baseline = true)]
    public int Default() => HttpCharacters.IndexOfInvalidTokenChar(this.TokenBytes);

    [Benchmark]
    public int Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidTokenChar(this.TokenBytes);
}
