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
        "System.Runtime.CompilerServices"
    };

    private static readonly string[] s_warningPragmas =
    {
        "CS0219  // The variable '...' is assigned but its value is never used"
    };
    //-------------------------------------------------------------------------
    public void Emit(SourceProductionContext context, ImmutableArray<IndexOfAnyMethod> methods)
    {
        //System.Diagnostics.Debugger.Launch();

        StringBuilder buffer = new();

        IEnumerable<IGrouping<ContainingTypeInfo, MethodInfo>> methodGroups = methods.GroupBy(m => m.Type, m => m.Method);
        foreach (IGrouping<ContainingTypeInfo, MethodInfo> methodGroup in methodGroups)
        {
            ContainingTypeInfo containingType = methodGroup.Key;
            string code                       = this.GenerateCode(containingType, methodGroup.ToArray(), buffer);
            string fileName                   = GetFilename(containingType, buffer);

            context.AddSource(fileName, code);
        }
    }
    //-------------------------------------------------------------------------
    private static string GetFilename(ContainingTypeInfo typeInfo, StringBuilder buffer)
    {
        buffer.Clear();

        if (typeInfo.Namespace is { } ns)
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }
        buffer.Append(typeInfo.Name);
        buffer.Append("_GeneratedIndexOfAny.g.cs");

        return buffer.ToString();
    }
    //-------------------------------------------------------------------------
    private string GenerateCode(ContainingTypeInfo typeInfo, MethodInfo[] methods, StringBuilder buffer)
    {
        buffer.Clear();
        using StringWriter sw           = new(buffer);
        using IndentedTextWriter writer = new(sw);

        foreach (string header in Globals.Headers)
        {
            writer.WriteLine(header);
        }

        EmitWarningPragmas(writer, disable: true);
        EmitNamespaces(writer);
        writer.WriteLine();

        if (typeInfo.Namespace is not null)
        {
            writer.WriteLine($"namespace {typeInfo.Namespace};");
            writer.WriteLine();
        }

        writer.Write($"partial {typeInfo.TypeKind} ");
        writer.WriteLine($"{typeInfo.Name}");
        writer.WriteLine("{");
        writer.Indent++;

        for (int i = 0; i < methods.Length; ++i)
        {
            this.EmitMethod(writer, methods[i]);

            if (i < methods.Length - 1)
            {
                writer.WriteLine("//-------------------------------------------------------------------------");
            }
        }

        writer.Indent--;
        writer.WriteLine("}");

        EmitWarningPragmas(writer, disable: false);

        return sw.ToString();
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
            EmitMethodBody(writer, methodInfo);
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
    private static void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        writer.WriteLine($"""return value.IndexOfAny("{methodInfo.SetChars}");""");
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
