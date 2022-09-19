// (c) gfoidl, all rights reserved

using Demo.Internal;

string s = "abcdefgh8ijklmnopqrstuv3wxyz";
ReadOnlySpan<char> span = s;

Console.WriteLine($"{span.IndexOfAny("1234567890")} expected: 8");
Console.WriteLine($"{Demo1.FirstIndexOfNumber(span)} expected: 8");
Console.WriteLine($"{Demo1.FirstIndexOfSet0(span)} expected: 3");
Console.WriteLine(Demo1.FirstIndexOfNonAsciiSet(span));

Console.WriteLine(Demo2.FirstIndexOfNotNumber(span));
Console.WriteLine($"{Demo2.FirstIndexOfNotNumber1(span)} expected: 7"); 

internal static partial class Demo1
{
    [GeneratedIndexOfAny("1234567890")]
    public static partial int FirstIndexOfNumber(ReadOnlySpan<char> value);

    [GeneratedIndexOfAny("drvxyz")]
    public static partial int FirstIndexOfSet0(ReadOnlySpan<char> value);

    [GeneratedIndexOfAny("12🌄34")]
    public static partial int FirstIndexOfNonAsciiSet(ReadOnlySpan<char> value);
}

namespace Demo.Internal
{
    internal static partial class Demo2
    {
        [GeneratedIndexOfAny("abcd", FindAnyExcept = true)]
        public static partial int FirstIndexOfNotNumber(ReadOnlySpan<char> value);

        [GeneratedIndexOfAny("abcdef", FindAnyExcept = true)]
        public static partial int FirstIndexOfNotNumber1(ReadOnlySpan<char> value);
    }
}
