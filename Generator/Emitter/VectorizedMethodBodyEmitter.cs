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
        this.EmitConstants(writer);
        writer.WriteLine();

        string valueParameterName = _methodInfo.Parameters[0].Name;
        string negator            = _methodInfo.IndexOfAnyOptions.FindAnyExcept ? "Negate" : "DontNegate";

        writer.WriteLine($"""
            return Vector128.IsHardwareAccelerated && {valueParameterName}.Length >= Vector128<short>.Count
                            ? Core.IndexOfMatchCharVectorized<Core.{negator}>({valueParameterName}, mask)
                            : Core.IndexOfMatchCharScalar<Core.{negator}>({valueParameterName}, lookup);
            """);
        return true;
    }
    //-------------------------------------------------------------------------
    private void EmitConstants(IndentedTextWriter writer)
    {
        writer.WriteLine("ReadOnlySpan<bool> lookup = new bool[128];");
        writer.WriteLine("Vector128<sbyte> mask     = Vector128.Create((sbyte)0x42);");
    }
}
