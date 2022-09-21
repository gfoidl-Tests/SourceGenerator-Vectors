// (c) gfoidl, all rights reserved

using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using StrippedCoreLib;
using MyMemoryExtension = StrippedCoreLib.MemoryExtensionsWithInitData;

Console.WriteLine($"HW accelarated: {Vector128.IsHardwareAccelerated}");
Console.WriteLine();

Bench bench = new();
bench.Authority = "hostname:8080";
Console.WriteLine(bench.Authority);
Console.WriteLine(bench.WithInitData());
Console.WriteLine(bench.WithUInt128());
Console.WriteLine(bench.WithVector());
Console.WriteLine();
bench.Authority = "[fe80]";
Console.WriteLine(bench.WithInitData());
Console.WriteLine(bench.WithUInt128());
Console.WriteLine(bench.WithVector());

#if !DEBUG
BenchmarkDotNet.Running.BenchmarkRunner.Run<Bench>();
#endif

//[DisassemblyDiagnoser]
//[ShortRunJob]
public partial class Bench
{
    private const string AlphaNumeric        = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AuthoritySpecific   = ":.-[]@";
    private const string ValidAuthorityChars = AlphaNumeric + AuthoritySpecific;

    private static readonly MyMemoryExtension.IndexOfAnyInitData s_authorityInitData    = MyMemoryExtension.IndexOfAnyInitialize(ValidAuthorityChars);
    private static readonly UInt128                              s_authorityInitUInt128 = MemoryExtensionsWithUInt128.IndexOfAnyInitialize(ValidAuthorityChars);
    private static readonly Vector128<byte>                      s_authorityInitVector  = MemoryExtensionsWithVector.IndexOfAnyInitialize(ValidAuthorityChars);

    [Params("[fe80]", "hostname:8080")]
    public string Authority { get; set; } = "[fe80]";

    [Benchmark(Baseline = true)]
    public int WithInitData() => MemoryExtensionsWithInitData.IndexOfAnyExcept(this.Authority.AsSpan(), s_authorityInitData);

    [Benchmark]
    public int WithUInt128() => MemoryExtensionsWithUInt128.IndexOfAnyExcept(this.Authority.AsSpan(), s_authorityInitUInt128);

    [Benchmark]
    public int WithVector() => MemoryExtensionsWithVector.IndexOfAnyExcept(this.Authority.AsSpan(), s_authorityInitVector);
}
