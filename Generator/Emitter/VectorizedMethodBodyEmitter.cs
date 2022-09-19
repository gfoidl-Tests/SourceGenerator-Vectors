// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Generator.Models;

namespace Generator.Emitter;

internal sealed class VectorizedMethodBodyEmitter : MethodBodyEmitter
{
    public VectorizedMethodBodyEmitter(MethodInfo methodInfo) : base(methodInfo) { }
    //-------------------------------------------------------------------------
    public override bool Emit(IndentedTextWriter writer)
    {
        writer.WriteLine($"""return value.IndexOfAny("{_methodInfo.IndexOfAnyOptions.SetChars}");""");
        return true;
    }
}
