using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleApp2.Generator
{
    [Generator]
    public class HttpCharactersGenerator : ISourceGenerator
    {
        private const int TableSize = 128;

        private static readonly bool[] s_alphaNumeric = InitializeAlphaNumeric();
        private static readonly bool[] s_authority = InitializeAuthority();
        private static readonly bool[] s_token = InitializeToken();
        private static readonly bool[] s_host = InitializeHost();
        private static readonly bool[] s_fieldValue = InitializeFieldValue();
        //---------------------------------------------------------------------
        private static bool[] InitializeAlphaNumeric()
        {
            // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1

            bool[] alphaNumeric = new bool[TableSize];

            SetMask('0', '9');
            SetMask('A', 'Z');
            SetMask('a', 'z');

            void SetMask(char first, char last)
            {
                for (char c = first; c <= last; ++c)
                {
                    alphaNumeric[c] = true;
                }
            }

            return alphaNumeric;
        }

        private static bool[] InitializeAuthority()
        {
            // Authority https://tools.ietf.org/html/rfc3986#section-3.2
            // Examples:
            // microsoft.com
            // hostname:8080
            // [::]:8080
            // [fe80::]
            // 127.0.0.1
            // user@host.com
            // user:password@host.com

            bool[] authority = new bool[TableSize];
            Array.Copy(s_alphaNumeric, authority, TableSize);

            foreach (char c in ":.[]@")
            {
                authority[c] = true;
            }

            return authority;
        }

        private static bool[] InitializeToken()
        {
            // tchar https://tools.ietf.org/html/rfc7230#appendix-B

            bool[] token = new bool[TableSize];
            Array.Copy(s_alphaNumeric, token, TableSize);

            foreach (char c in "!#$%&\'*+-.^_`|~")
            {
                token[c] = true;
            }

            return token;
        }

        private static bool[] InitializeHost()
        {
            // Matches Http.Sys
            // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys

            bool[] host = new bool[TableSize];
            Array.Copy(s_alphaNumeric, host, TableSize);

            foreach (char c in "!$&\'()-._~")
            {
                host[c] = true;
            }

            return host;
        }

        private static bool[] InitializeFieldValue()
        {
            // field-value https://tools.ietf.org/html/rfc7230#section-3.2

            bool[] fieldValue = new bool[TableSize];

            for (var c = 0x20; c <= 0x7e; c++) // VCHAR and SP
            {
                fieldValue[c] = true;
            }

            return fieldValue;
        }
        //---------------------------------------------------------------------
        public void Initialize(GeneratorInitializationContext context)
        { }
        //---------------------------------------------------------------------
        public void Execute(GeneratorExecutionContext context)
        {
            StringBuilder builder = new();

            builder.Append(@"
using System;
using System.Runtime.CompilerServices;

[CompilerGenerated]
internal static partial class HttpCharacters_Vectorized
{
    private static class Constants
    {");

            EmitConstant(builder, "LookupAlphaNumeric", s_alphaNumeric);
            EmitConstant(builder, "LookupAuthority", s_authority);
            EmitConstant(builder, "LookupToken", s_token);
            EmitConstant(builder, "LookupHost", s_host);
            EmitConstant(builder, "LookupFieldValue", s_fieldValue);

            builder.Append(@"
    }

    // Due to a JIT limitation we need to slice these constants (see comments above) in order
    // to ""unlink"" the span, and allow proper hoisting out of the loop.
    // This is tracked in https://github.com/dotnet/runtime/issues/12241
");

            EmitLookup(builder, "LookupAlphaNumeric");
            EmitLookup(builder, "LookupAuthority");
            EmitLookup(builder, "LookupToken");
            EmitLookup(builder, "LookupHost");
            EmitLookup(builder, "LookupFieldValue");

            builder.Append(@"
}
");
            string code = builder.ToString();
            context.AddSource("HttpCharacters_Vectorized.generated.cs", SourceText.From(code, Encoding.UTF8));
        }
        //---------------------------------------------------------------------
        private void EmitLookup(StringBuilder builder, string name)
        {
            builder.Append($@"
    private static partial ReadOnlySpan<bool> {name}() => Constants.{name}.Slice(1);");
        }
        //---------------------------------------------------------------------
        private void EmitConstant(StringBuilder builder, string name, bool[] lookup)
        {
            builder.Append($@"
        public static ReadOnlySpan<bool> {name} => new bool[] {{ /* This is dummy to workaround a JIT limitation */ false, ");

            for (int i = 0; i < lookup.Length; ++i)
            {
                builder.Append(lookup[i] ? "true" : "false");

                if (i < lookup.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(" };");
        }
    }
}
