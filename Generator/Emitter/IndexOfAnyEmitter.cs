// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Generator.Models;
using Microsoft.CodeAnalysis;

namespace Generator.Emitter;

internal class IndexOfAnyEmitter
{
    private static readonly string[] s_namespaces =
    {
        "System",
        "System.CodeDom.Compiler",
        "System.ComponentModel",
        "System.Diagnostics",
        "System.Runtime.CompilerServices",
        "System.Runtime.Intrinsics",
        "System.Runtime.Intrinsics.X86"
    };

    private static readonly string[] s_warningPragmas =
    {
        "CS0219  // The variable '...' is assigned but its value is never used"
    };
    //-------------------------------------------------------------------------
    private bool _needToEmitVectorHelpers = false;
    //-------------------------------------------------------------------------
    public void Emit(SourceProductionContext context, ImmutableArray<IndexOfAnyMethod> methods)
    {
        //System.Diagnostics.Debugger.Launch();

        StringBuilder buffer            = new();
        using StringWriter sw           = new(buffer);
        using IndentedTextWriter writer = new(sw);

        EmitPrologue(writer);

        IEnumerable<IGrouping<ContainingTypeInfo, MethodInfo>> methodGroups = methods.GroupBy(m => m.Type, m => m.Method);
        foreach (IGrouping<ContainingTypeInfo, MethodInfo> methodGroup in methodGroups)
        {
            ContainingTypeInfo containingType = methodGroup.Key;
            this.EmitType(containingType, methodGroup.ToArray(), writer);
        }

        if (_needToEmitVectorHelpers)
        {
            HelpersEmitter.EmitVectorHelpers(writer);
        }

        EmitWarningPragmas(writer, disable: false);

        string code = sw.ToString();
        context.AddSource($"{IndexOfAnyGenerator.GeneratedIndexOfAnyAttributeName.Replace("Attribute", "Methods")}.g.cs", code);
    }
    //-------------------------------------------------------------------------
    private static void EmitPrologue(IndentedTextWriter writer)
    {
        foreach (string header in Globals.Headers)
        {
            writer.WriteLine(header);
        }

        EmitWarningPragmas(writer, disable: true);
        EmitNamespaces(writer);
    }
    //-------------------------------------------------------------------------
    private static void EmitWarningPragmas(IndentedTextWriter writer, bool disable)
    {
        string text = $"#pragma warning {(disable ? "disable" : "restore")} ";

        writer.WriteLine();
        foreach (string pragma in s_warningPragmas)
        {
            writer.WriteLine(text + pragma);
        }

        if (disable)
        {
            writer.WriteLine();
        }
    }
    //-------------------------------------------------------------------------
    private static void EmitNamespaces(IndentedTextWriter writer)
    {
        foreach (string ns in (IEnumerable<string>)s_namespaces)
        {
            writer.WriteLine($"using {ns};");
        }
    }
    //-------------------------------------------------------------------------
    private void EmitType(ContainingTypeInfo typeInfo, MethodInfo[] methods, IndentedTextWriter writer)
    {
        writer.WriteLine();

        if (typeInfo.Namespace is not null)
        {
            writer.WriteLine($"namespace {typeInfo.Namespace}");
            writer.WriteLine("{");
            writer.Indent++;
        }

        writer.Write($"partial {typeInfo.TypeKind} ");
        writer.WriteLine($"{typeInfo.Name}");
        writer.WriteLine("{");
        writer.Indent++;

        for (int i = 0; i < methods.Length; ++i)
        {
            this.EmitMethod(writer, methods[i]);
        }

        writer.Indent--;
        writer.WriteLine("}");

        if (typeInfo.Namespace is not null)
        {
            writer.Indent--;
            writer.WriteLine("}");
        }
    }
    //-------------------------------------------------------------------------
    private void EmitMethod(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        writer.WriteLine($"[{Globals.GeneratedCodeAttribute}]");
        writer.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
        writer.WriteLine("[DebuggerNonUserCode]");

        writer.Write(AccessibilityText(methodInfo.Accessibility));
        if (methodInfo.IsStatic)
        {
            writer.Write(" static");
        }
        writer.Write($" partial int {methodInfo.Name}(");
        EmitParameters(writer, methodInfo);
        writer.WriteLine(")");
        writer.WriteLine("{");
        writer.Indent++;
        {
            this.EmitMethodBody(writer, methodInfo);
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
    //-------------------------------------------------------------------------
    private static void EmitParameters(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        for (int i = 0; i < methodInfo.Parameters.Length; ++i)
        {
            ParameterInfo parameter = methodInfo.Parameters[i];

            writer.Write($"{parameter.Type.ToDisplayString()} {parameter.Name}");

            if (i < methodInfo.Parameters.Length - 1)
            {
                writer.Write(", ");
            }
        }
    }
    //-------------------------------------------------------------------------
    private void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        MethodBodyEmitter bodyEmitter = MethodBodyEmitter.Create(methodInfo);
        _needToEmitVectorHelpers |= bodyEmitter.Emit(writer);
    }
    //-------------------------------------------------------------------------
    private static string AccessibilityText(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public               => "public",
        Accessibility.Protected            => "protected",
        Accessibility.Private              => "private",
        Accessibility.Internal             => "internal",
        Accessibility.ProtectedOrInternal  => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _                                  => throw new InvalidOperationException(),
    };
}
