// (c) gfoidl, all rights reserved

string s = "abcdefgh8ijklmnopqrstuv3wxyz";
ReadOnlySpan<char> span = s;

Console.WriteLine(span.IndexOfAny("1234567890"));
Console.WriteLine(Demo.FirstIndexOfNumber(span));

internal static partial class Demo
{
    [GeneratedIndexOfAny("[0-9]")]
    public static partial int FirstIndexOfNumber(ReadOnlySpan<char> value);
}
