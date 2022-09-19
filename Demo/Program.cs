// (c) gfoidl, all rights reserved

using StrippedCoreLib;

ReadOnlySpan<char> span = "abcdefgh8ijklmnopqrstuv3wxyz";

Console.WriteLine($"{span.IndexOfAny("1234567890")} expected: 8");
Console.WriteLine($"{Demo.FirstIndexOfNumber(span)} expected: 8");
Console.WriteLine();
Console.WriteLine($"{span.IndexOfAny("drvxyz")} expected: 3");
Console.WriteLine($"{Demo.FirstIndexOfSet0(span)} expected: 3");
Console.WriteLine();
Console.WriteLine(span.IndexOfAny("12🌄34"));
Console.WriteLine(Demo.FirstIndexOfNonAsciiSet(span));
Console.WriteLine();
Console.WriteLine(span.IndexOfAnyExcept("abcd"));
Console.WriteLine(Demo.FirstIndexOfNotSet0(span));
Console.WriteLine();
Console.WriteLine($"{span.IndexOfAnyExcept("abcdef")} expected: 6");
Console.WriteLine($"{Demo.FirstIndexOfNotSet1(span)} expected: 6");

internal static class Demo
{
    private static readonly MemoryExtensions1.IndexOfAnyInitData s_firstIndexOfNumberInitData = MemoryExtensions1.IndexOfAnyInitialize("1234567890");
    private static readonly MemoryExtensions1.IndexOfAnyInitData s_firstIndexOfSet0           = MemoryExtensions1.IndexOfAnyInitialize("drvxyz");
    private static readonly MemoryExtensions1.IndexOfAnyInitData s_firstIndexOfNonAsciiSet    = MemoryExtensions1.IndexOfAnyInitialize("12🌄34");

    public static int FirstIndexOfNumber(ReadOnlySpan<char> value)      => value.IndexOfAny("1234567890", s_firstIndexOfNumberInitData);
    public static int FirstIndexOfSet0(ReadOnlySpan<char> value)        => value.IndexOfAny("drvxyz", s_firstIndexOfSet0);
    public static int FirstIndexOfNonAsciiSet(ReadOnlySpan<char> value) => value.IndexOfAny("12🌄34", s_firstIndexOfNonAsciiSet);

    private static readonly MemoryExtensions1.IndexOfAnyInitData s_firstIndexOfNotSet0 = MemoryExtensions1.IndexOfAnyInitialize("abcd");
    private static readonly MemoryExtensions1.IndexOfAnyInitData s_firstIndexOfNotSet1 = MemoryExtensions1.IndexOfAnyInitialize("abcdef");

    public static int FirstIndexOfNotSet0(ReadOnlySpan<char> value) => value.IndexOfAnyExcept("abcd", s_firstIndexOfNotSet0);
    public static int FirstIndexOfNotSet1(ReadOnlySpan<char> value) => value.IndexOfAnyExcept("abcdef", s_firstIndexOfNotSet1);
}
