// (c) gfoidl, all rights reserved

namespace Generator;

internal static class Globals
{
    public static string GeneratedCodeAttribute { get; } = $"GeneratedCode(\"{typeof(IndexOfAnyGenerator).Assembly.GetName().Name}\", \"{typeof(IndexOfAnyGenerator).Assembly.GetName().Version}\")";
    //-------------------------------------------------------------------------
    public static string[] Headers { get; } = new string[]
    {
        "// <auto-generated />",
        "#nullable enable"
    };
}
