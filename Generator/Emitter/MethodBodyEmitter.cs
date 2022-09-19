// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Generator.Models;

namespace Generator.Emitter;

internal abstract class MethodBodyEmitter
{
    protected readonly MethodInfo _methodInfo;
    //-------------------------------------------------------------------------
    protected MethodBodyEmitter(MethodInfo methodInfo) => _methodInfo = methodInfo;
    //-------------------------------------------------------------------------
    public static MethodBodyEmitter Create(MethodInfo methodInfo, IndentedTextWriter writer)
    {
        string setChars = methodInfo.IndexOfAnyOptions.SetChars;

        // TODO: 5 is the current threshold for specialized IndexOfAny{Expect}, should there be a constant exposed for this in MemoryExtensions?
        // TODO: evaluate if 5 is still the best break-even with the vectorized approach here
        if (setChars.Length <= 5)
        {
            writer.WriteLine("// Given SetChars <= 5, so use specialized method from IndexOfAny{Except}");
            return new FallbackMethodBodyEmitter(methodInfo);
        }

        if (!IsAllAscii(setChars))
        {
            writer.WriteLine("// Given SetChars consist not of all ASCII, can't handle vectorized, so use fallback");
            return new FallbackMethodBodyEmitter(methodInfo);
        }

        return new VectorizedMethodBodyEmitter(methodInfo);
    }
    //-------------------------------------------------------------------------
    private static bool IsAllAscii(string setChars)
    {
        foreach (char c in setChars)
        {
            if (c > 0x7F)
            {
                return false;
            }
        }

        return true;
    }
    //-------------------------------------------------------------------------
    public abstract bool Emit(IndentedTextWriter writer);
}
