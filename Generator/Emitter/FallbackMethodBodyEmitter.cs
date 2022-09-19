// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Generator.Models;

namespace Generator.Emitter;

internal sealed class FallbackMethodBodyEmitter : MethodBodyEmitter
{
    public FallbackMethodBodyEmitter(MethodInfo methodInfo) : base(methodInfo) { }
    //-------------------------------------------------------------------------
    public override bool Emit(IndentedTextWriter writer)
    {
        string fallbackMethod = this.GetFallbackMethod();
        writer.WriteLine($"""return value.{fallbackMethod}("{_methodInfo.IndexOfAnyOptions.SetChars}");""");
        return false;
    }
    //-------------------------------------------------------------------------
    private string GetFallbackMethod()
    {
        return _methodInfo.IndexOfAnyOptions.FindAnyExcept
            ? "IndexOfAnyExcept"
            : "IndexOfAny";
    }
}
