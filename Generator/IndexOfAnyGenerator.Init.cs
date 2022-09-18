// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Generator;

public partial class IndexOfAnyGenerator
{
    private const string AttributeCode = $$"""
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public class {{GeneratedIndexOfAnyAttributeName}} : Attribute
        {
            public string SetChars    { get; }
            public bool FindAnyExcept { get; set; }
            //-------------------------------------------------------------------------
            public {{GeneratedIndexOfAnyAttributeName}}(string setChars) => this.SetChars = setChars;
        }
        """;
    //-------------------------------------------------------------------------
    private static void AddAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        using StringWriter sw           = new();
        using IndentedTextWriter writer = new(sw);

        foreach (string header in Globals.Headers)
        {
            writer.WriteLine(header);
        }
        writer.WriteLine();

        writer.WriteLine("using System;");
        writer.WriteLine("using System.ComponentModel;");
        writer.WriteLine("using System.CodeDom.Compiler;");
        writer.WriteLine();
        writer.WriteLine($"[{Globals.GeneratedCodeAttribute}]");
        writer.WriteLine("[EditorBrowsable(EditorBrowsableState.Always)]");
        writer.WriteLine(AttributeCode);

        string code = sw.ToString();
        context.AddSource($"{GeneratedIndexOfAnyAttributeName}.g.cs", code);
    }
}
