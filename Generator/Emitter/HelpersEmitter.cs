// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;

namespace Generator.Emitter;

internal static class HelpersEmitter
{
    public static void EmitVectorHelpers(IndentedTextWriter writer)
    {
        writer.WriteLine();

        writer.WriteLine("file class VectorHelper");
        writer.WriteLine("{");
        writer.Indent++;
        {
            writer.WriteLine("// TODO");
        }
        writer.Indent--;
        writer.WriteLine("}");

        writer.WriteLine();
    }
}
