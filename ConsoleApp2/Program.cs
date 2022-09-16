#define PRINT_DASM

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

Bench bench = new();
bench.GlobalSetup();
Console.WriteLine(bench.Token.Length);
//Console.WriteLine(bench.TokenBytes.Length);
Console.WriteLine();

Console.WriteLine(bench.Default());
Console.WriteLine(bench.Vectorized());

#if !DEBUG
#if PRINT_DASM
Console.WriteLine(new string('#', 100));
const int N = 100;
for (int i = 0; i < N; ++i)
{
    _ = bench.Vectorized();

    if (i % 10 == 0)
    {
        await Task.Delay(150);
    }
}
#else 
    BenchmarkDotNet.Running.BenchmarkRunner.Run<Bench>();
#endif
#endif

[ShortRunJob]
[DisassemblyDiagnoser]
public class Bench
{
    //[Params("abc", "0123456789abcd❤efghij", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa❤aaaaaa")]
    public string Token { get; set; } = "0123456789abcdefghij❤k";
    //public string Token { get; set; } = "microsoft.com";

    public void GlobalSetup() { }

    public Bench()
    {
        HttpCharacters.Initialize();
        HttpCharacters_BitArray.Initialize();
    }

    //[Benchmark(Baseline = true)]
    public int Default() => HttpCharacters.IndexOfInvalidFieldValueCharExtended(this.Token);

    //[Benchmark]
    public int BitArrayBased() => HttpCharacters_BitArray.IndexOfInvalidHostChar(this.Token);

    [Benchmark]
    public int Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidFieldValueCharExtended(this.Token);
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
