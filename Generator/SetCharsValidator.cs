// (c) gfoidl, all rights reserved

using System.Diagnostics;

namespace Generator;

internal static class SetCharsValidator
{
    public static bool IsSetCharsValid(ReadOnlySpan<char> setChars)
    {
        Debug.Assert(!setChars.IsEmpty);

        // TODO: implement validation

        if (setChars[0] == '[')
        {
            return RangeValidation(setChars);
        }

        return true;
    }
    //-------------------------------------------------------------------------
    private static bool RangeValidation(ReadOnlySpan<char> setChars)
    {
        if (setChars[setChars.Length - 1] != ']')
        {
            return false;
        }

        return true;
    }
}
