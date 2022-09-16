using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

Benchmarks bench = new()
{
    Length         = 15,
    InvalidCharPos = InvalidCharPos.Mid
};
bench.Setup();
Console.WriteLine(bench.ContainsInvalidAuthorityChar_Default());
Console.WriteLine(bench.ContainsInvalidAuthorityChar_Vectorized());

#if !DEBUG
BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarks>();
#endif

[ShortRunJob]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class Benchmarks
{
    private const char Invalid = (char)1;

    [Params(7, 8, 15, 16, 113)]
    public int Length { get; set; }

    [Params(InvalidCharPos.None, InvalidCharPos.Start, InvalidCharPos.Mid, InvalidCharPos.End)]
    public InvalidCharPos InvalidCharPos { get; set; }

    private string? _value;
    private byte[]? _bytes;

    [GlobalSetup]
    public void Setup()
    {
        char[] chars = new char[this.Length];
        chars.AsSpan().Fill('a');

        if (this.InvalidCharPos == InvalidCharPos.Start)
        {
            chars[0] = Invalid;
        }
        else if (this.InvalidCharPos == InvalidCharPos.Mid)
        {
            chars[this.Length / 2] = Invalid;
        }
        else if (this.InvalidCharPos == InvalidCharPos.End)
        {
            chars[^1] = Invalid;
        }

        _value = new string(chars);
        _bytes = Encoding.UTF8.GetBytes(_value);
    }

    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("ContainsInvalidAuthorityChar")]
    public bool ContainsInvalidAuthorityChar_Default() => HttpCharacters.ContainsInvalidAuthorityChar(_bytes);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("ContainsInvalidAuthorityChar")]
    public bool ContainsInvalidAuthorityChar_Vectorized() => HttpCharacters_Vectorized.ContainsInvalidAuthorityChar(_bytes);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("IndexOfInvalidHostChar")]
    public int IndexOfInvalidHostChar_Default() => HttpCharacters.IndexOfInvalidHostChar(_value);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("IndexOfInvalidHostChar")]
    public int IndexOfInvalidHostChar_Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidHostChar(_value);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("IndexOfInvalidTokenChar_String")]
    public int IndexOfInvalidTokenChar_String_Default() => HttpCharacters.IndexOfInvalidTokenChar(_value);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("IndexOfInvalidTokenChar_String")]
    public int IndexOfInvalidTokenChar_String_Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidTokenChar(_value);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("IndexOfInvalidTokenChar_Bytes")]
    public int IndexOfInvalidTokenChar_Bytes_Default() => HttpCharacters.IndexOfInvalidTokenChar(_bytes);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("IndexOfInvalidTokenChar_Bytes")]
    public int IndexOfInvalidTokenChar_Bytes_Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidTokenChar(_bytes);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("IndexOfInvalidFieldValueChar")]
    public int IndexOfInvalidFieldValueChar_Default() => HttpCharacters.IndexOfInvalidFieldValueChar(_value);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("IndexOfInvalidFieldValueChar")]
    public int IndexOfInvalidFieldValueChar() => HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(_value);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Default")]
    [BenchmarkCategory("IndexOfInvalidFieldValueCharExtended")]
    public int IndexOfInvalidFieldValueCharExtended_Default() => HttpCharacters.IndexOfInvalidFieldValueCharExtended(_value);

    [Benchmark(Description = "Vectorized")]
    [BenchmarkCategory("IndexOfInvalidFieldValueCharExtended")]
    public int IndexOfInvalidFieldValueCharExtended_Vectorized() => HttpCharacters_Vectorized.IndexOfInvalidFieldValueCharExtended(_value);
}

public enum InvalidCharPos
{
    None,
    Start,
    Mid,
    End
}
