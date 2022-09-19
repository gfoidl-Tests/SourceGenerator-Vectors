// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Diagnostics;
using Generator.Models;

namespace Generator.Emitter;

internal sealed unsafe class VectorizedMethodBodyEmitter : MethodBodyEmitter
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
        var (lookup, mask) = GetValues();

        EmitConstant  (writer, lookup);
        EmitVectorMask(writer, mask);
    }
    //-------------------------------------------------------------------------
    private static void EmitConstant(IndentedTextWriter writer, bool[] lookup)
    {
        writer.Write("ReadOnlySpan<bool> lookup = new bool[128] { ");

        for (int i = 0; i < lookup.Length; ++i)
        {
            writer.Write(lookup[i] ? "true" : "false");

            if (i < lookup.Length - 1)
            {
                writer.Write(", ");
            }
        }

        writer.WriteLine(" };");
    }
    //-------------------------------------------------------------------------
    private static void EmitVectorMask(IndentedTextWriter writer, sbyte[] mask)
    {
        writer.Write("Vector128<sbyte> mask     = Vector128.Create(");

        for (int i = 0; i < mask.Length; ++i)
        {
            writer.Write("0x{0:X2}", mask[i]);

            if (i < mask.Length - 1)
            {
                writer.Write(", ");
            }
        }

        writer.WriteLine(").AsSByte();");
    }
    //-------------------------------------------------------------------------
    private (bool[], sbyte[]) GetValues()
    {
        const int TableSize = 128;

        bool[] lookup = new bool[TableSize];
        sbyte[] mask  = new sbyte[TableSize / 8];

        foreach (char c in SetCharsParser.GetChars(_methodInfo.IndexOfAnyOptions.SetChars))
        {
            Debug.Assert(c < 128, "If non-ASCII the fallback should have been called");

            lookup[c] = true;
            SetBitInMask(mask, c);
        }

        return (lookup, mask);
    }
    //-------------------------------------------------------------------------
    private static void SetBitInMask(sbyte[] mask, int c)
    {
        Debug.Assert(c < 128);

        int highNibble = c >> 4;
        int lowNibble  = c & 0xF;

        mask[lowNibble] |= (sbyte)(1 << highNibble);
    }
}
