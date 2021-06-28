using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleApp2.Generator
{
    [Generator]
    public unsafe class HttpCharactersGenerator : ISourceGenerator
    {
        private const int TableSize = 128;

        private static readonly bool[] s_alphaNumeric;
        private static readonly bool[] s_authority;
        private static readonly bool[] s_token;
        private static readonly bool[] s_host;
        private static readonly bool[] s_fieldValue;

        private static readonly sbyte[] s_bitMaskLookupAlphaNumeric;
        private static readonly sbyte[] s_bitMaskLookupAuthority;
        private static readonly sbyte[] s_bitMaskLookupToken;
        private static readonly sbyte[] s_bitMaskLookupHost;
        private static readonly sbyte[] s_bitMaskLookupFieldValue;
        //---------------------------------------------------------------------
        static HttpCharactersGenerator()
        {
            (s_alphaNumeric, s_bitMaskLookupAlphaNumeric) = InitializeAlphaNumeric();
            (s_authority, s_bitMaskLookupAuthority) = InitializeAuthority();
            (s_token, s_bitMaskLookupToken) = InitializeToken();
            (s_host, s_bitMaskLookupHost) = InitializeHost();
            (s_fieldValue, s_bitMaskLookupFieldValue) = InitializeFieldValue();
        }
        //---------------------------------------------------------------------
        private static void SetBitInMask(sbyte* mask, int c)
        {
            Debug.Assert(c < 128);

            int highNibble = c >> 4;
            int lowNibble = c & 0xF;

            mask[lowNibble] &= (sbyte)~(1 << highNibble);
        }
        //---------------------------------------------------------------------
        private static (bool[], sbyte[]) InitializeAlphaNumeric()
        {
            // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1

            bool[] alphaNumeric = new bool[TableSize];
            sbyte[] vector = new sbyte[128 / 8];

            for (int i = 0; i < vector.Length; ++i)
            {
                vector[i] = -1;
            }

            fixed (sbyte* mask = vector)
            {
                SetMask(mask, '0', '9');
                SetMask(mask, 'A', 'Z');
                SetMask(mask, 'a', 'z');
            }

            return (alphaNumeric, vector);

            void SetMask(sbyte* mask, char first, char last)
            {
                for (char c = first; c <= last; ++c)
                {
                    alphaNumeric[c] = true;
                    SetBitInMask(mask, c);
                }
            }
        }

        private static (bool[], sbyte[]) InitializeAuthority()
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

            sbyte[] vector = new sbyte[128 / 8];
            Array.Copy(s_bitMaskLookupAlphaNumeric, vector, vector.Length);

            fixed (sbyte* mask = vector)
            {
                foreach (char c in ":.[]@")
                {
                    authority[c] = true;
                    SetBitInMask(mask, c);
                }
            }

            return (authority, vector);
        }

        private static (bool[], sbyte[]) InitializeToken()
        {
            // tchar https://tools.ietf.org/html/rfc7230#appendix-B

            bool[] token = new bool[TableSize];
            Array.Copy(s_alphaNumeric, token, TableSize);

            sbyte[] vector = new sbyte[128 / 8];
            Array.Copy(s_bitMaskLookupAlphaNumeric, vector, vector.Length);

            fixed (sbyte* mask = vector)
            {
                foreach (char c in "!#$%&\'*+-.^_`|~")
                {
                    token[c] = true;
                    SetBitInMask(mask, c);
                }
            }

            return (token, vector);
        }

        private static (bool[], sbyte[]) InitializeHost()
        {
            // Matches Http.Sys
            // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys

            bool[] host = new bool[TableSize];
            Array.Copy(s_alphaNumeric, host, TableSize);

            sbyte[] vector = new sbyte[128 / 8];
            Array.Copy(s_bitMaskLookupAlphaNumeric, vector, vector.Length);

            fixed (sbyte* mask = vector)
            {
                foreach (char c in "!$&\'()-._~")
                {
                    host[c] = true;
                    SetBitInMask(mask, c);
                }
            }

            return (host, vector);
        }

        private static (bool[], sbyte[]) InitializeFieldValue()
        {
            // field-value https://tools.ietf.org/html/rfc7230#section-3.2

            bool[] fieldValue = new bool[TableSize];
            sbyte[] vector = new sbyte[128 / 8];

            for (int i = 0; i < vector.Length; ++i)
            {
                vector[i] = -1;
            }

            fixed (sbyte* mask = vector)
            {
                for (var c = 0x20; c <= 0x7e; c++) // VCHAR and SP
                {
                    fieldValue[c] = true;
                    SetBitInMask(mask, c);
                }
            }

            return (fieldValue, vector);
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
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

[CompilerGenerated]
internal static partial class HttpCharacters_Vectorized
{");

            EmitLookup(builder, "LookupAlphaNumeric", s_alphaNumeric);
            EmitLookup(builder, "LookupAuthority", s_authority);
            EmitLookup(builder, "LookupToken", s_token);
            EmitLookup(builder, "LookupHost", s_host);
            EmitLookup(builder, "LookupFieldValue", s_fieldValue);

            builder.AppendLine();

            EmitVector(builder, "BitmaskAlphaNumeric", s_bitMaskLookupAlphaNumeric);
            EmitVector(builder, "BitMaskLookupAuthority", s_bitMaskLookupAuthority);
            EmitVector(builder, "BitMaskLookupToken", s_bitMaskLookupToken);
            EmitVector(builder, "BitMaskLookupHost", s_bitMaskLookupHost);
            EmitVector(builder, "BitMaskLookupFieldValue", s_bitMaskLookupFieldValue);

            builder.Append(@"
}
");
            string code = builder.ToString();
            context.AddSource("HttpCharacters_Vectorized.generated.cs", SourceText.From(code, Encoding.UTF8));
        }
        //---------------------------------------------------------------------
        private void EmitLookup(StringBuilder builder, string name, bool[] lookup)
        {
            builder.Append($@"
    private static partial ReadOnlySpan<bool> {name}() => new bool[] {{ ");

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
        //---------------------------------------------------------------------
        private void EmitVector(StringBuilder builder, string name, sbyte[] mask)
        {
            builder.Append($@"
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static partial Vector128<sbyte> {name}() => Vector128.Create(");

            for (int i = 0; i < mask.Length; ++i)
            {
                builder.Append("0x").AppendFormat("{0:X2}", mask[i]);

                if (i < mask.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(").AsSByte();");
        }
    }
}
