// (c) gfoidl, all rights reserved

using BenchmarkDotNet.Attributes;

Bench bench = new();
Console.WriteLine(bench.Authority);
Console.WriteLine(bench.IsValidByIndexOfAnyExcept());
Console.WriteLine(bench.IsValidByGeneratedIndexOfAnyExcept());

#if !DEBUG
BenchmarkDotNet.Running.BenchmarkRunner.Run<Bench>();
#endif

public partial class Bench
{
    private const string AlphaNumeric        = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AuthoritySpecific   = ":.-[]@";
    private const string ValidAuthorityChars = AlphaNumeric + AuthoritySpecific;

    [Params("hostname:8080", "www.thelongestdomainnameintheworldandthensomeandthensomemoreandmore.com")]
    public string Authority { get; set; } = "hostname:8080";

    [Benchmark(Baseline = true)]
    public bool IsValidByIndexOfAnyExcept() => this.Authority.AsSpan().IndexOfAnyExcept(ValidAuthorityChars) < 0;

    [Benchmark]
    public bool IsValidByGeneratedIndexOfAnyExcept() => IsAuthorityValid(this.Authority) < 0;

    [GeneratedIndexOfAny(ValidAuthorityChars, FindAnyExcept = true)]
    private static partial int IsAuthorityValid(ReadOnlySpan<char> host);
}
