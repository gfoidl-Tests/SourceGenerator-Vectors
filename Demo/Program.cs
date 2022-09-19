// (c) gfoidl, all rights reserved

using Demo.Internal;

string s = "abcdefgh8ijklmnopqrstuv3wxyz";
ReadOnlySpan<char> span = s;

Console.WriteLine(span.IndexOfAny("1234567890"));
Console.WriteLine(Demo1.FirstIndexOfNumber(span));
Console.WriteLine(Demo2.FirstIndexOfNotNumber(span));

internal static partial class Demo1
{
    [GeneratedIndexOfAny("1234567890")]
    public static partial int FirstIndexOfNumber(ReadOnlySpan<char> value);
}

namespace Demo.Internal
{
    internal static partial class Demo2
    {
        [GeneratedIndexOfAny("1234567890", FindAnyExcept = true)]
        public static partial int FirstIndexOfNotNumber(ReadOnlySpan<char> value);
    }
}
