// (c) gfoidl, all rights reserved

namespace Generator.Emitter;

internal static class SetCharsParser
{
    public static IEnumerable<char> GetChars(string setChars)
    {
        // TODO: right now the setChars is simple -- just the list of chars
        // There might be regex-like set grammar, e.g. [a-z] to allow all lowercase chars
        // instead of giving them as "huge" string.
        //
        // That parsing logic should reside here.
        return setChars;
    }
}
