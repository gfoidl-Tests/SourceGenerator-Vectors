// (c) gfoidl, all rights reserved

using Demo.Internal;

ReadOnlySpan<char> span = "abcdefgh8ijklmnopqrstuv3wxyz";

Console.WriteLine($"{span.IndexOfAny("1234567890")} expected: 8");
Console.WriteLine($"{Demo1.FirstIndexOfNumber(span)} expected: 8");
Console.WriteLine($"{Demo1.FirstIndexOfSet0(span)} expected: 3");
Console.WriteLine(Demo1.FirstIndexOfNonAsciiSet(span));

Console.WriteLine(Demo2.FirstIndexOfNotSet0(span));
Console.WriteLine($"{Demo2.FirstIndexOfNotSet1(span)} expected: 6");

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
        public static partial int FirstIndexOfNotSet0(ReadOnlySpan<char> value);

        [GeneratedIndexOfAny("abcdef", FindAnyExcept = true)]
        public static partial int FirstIndexOfNotSet1(ReadOnlySpan<char> value);
    }
}
