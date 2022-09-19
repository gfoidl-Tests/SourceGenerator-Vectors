// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;

namespace Generator.Emitter;

internal static class HelpersEmitter
{
    public static void EmitVectorHelpers(IndentedTextWriter writer)
    {
        writer.WriteLine();

        // TODO: should be 'file class' but somehow this results in
        // "CS0116: A namespace cannot directly contain members such as fields, methods or statements"
        //writer.WriteLine("file class VectorHelper");
        writer.WriteLine("class VectorHelper");
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
