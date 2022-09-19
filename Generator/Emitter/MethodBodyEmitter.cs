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
    public static MethodBodyEmitter Create(MethodInfo methodInfo)
    {
        return new VectorizedMethodBodyEmitter(methodInfo);
    }
    //-------------------------------------------------------------------------
    public abstract bool Emit(IndentedTextWriter writer);
}
