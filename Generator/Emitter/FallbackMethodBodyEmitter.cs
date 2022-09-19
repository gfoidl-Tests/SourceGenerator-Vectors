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
        writer.WriteLine($"""return value.IndexOfAny("{_methodInfo.IndexOfAnyOptions.SetChars}");""");
        return false;
    }
}
