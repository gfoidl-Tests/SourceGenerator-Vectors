// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal static class HttpCharacters
{
    private const int TableSize = 128;
    private static readonly bool[] s_alphaNumeric = InitializeAlphaNumeric();
    private static readonly bool[] s_authority = InitializeAuthority();
    private static readonly bool[] s_token = InitializeToken();
    private static readonly bool[] s_host = InitializeHost();
    private static readonly bool[] s_fieldValue = InitializeFieldValue();

    internal static void Initialize()
    {
        // Access _alphaNumeric to initialize static fields
        _ = s_alphaNumeric;
    }

    private static bool[] InitializeAlphaNumeric()
    {
        // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1
        bool[] alphaNumeric = new bool[TableSize];
        for (char c = '0'; c <= '9'; c++)
        {
            alphaNumeric[c] = true;
        }
        for (char c = 'A'; c <= 'Z'; c++)
        {
            alphaNumeric[c] = true;
        }
        for (char c = 'a'; c <= 'z'; c++)
        {
            alphaNumeric[c] = true;
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
        authority[':'] = true;
        authority['.'] = true;
        authority['-'] = true;
        authority['['] = true;
        authority[']'] = true;
        authority['@'] = true;
        return authority;
    }

    private static bool[] InitializeToken()
    {
        // tchar https://tools.ietf.org/html/rfc7230#appendix-B
        bool[] token = new bool[TableSize];
        Array.Copy(s_alphaNumeric, token, TableSize);
        token['!'] = true;
        token['#'] = true;
        token['$'] = true;
        token['%'] = true;
        token['&'] = true;
        token['\''] = true;
        token['*'] = true;
        token['+'] = true;
        token['-'] = true;
        token['.'] = true;
        token['^'] = true;
        token['_'] = true;
        token['`'] = true;
        token['|'] = true;
        token['~'] = true;
        return token;
    }

    private static bool[] InitializeHost()
    {
        // Matches Http.Sys
        // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys
        bool[] host = new bool[TableSize];
        Array.Copy(s_alphaNumeric, host, TableSize);
        host['!'] = true;
        host['$'] = true;
        host['&'] = true;
        host['\''] = true;
        host['('] = true;
        host[')'] = true;
        host['-'] = true;
        host['.'] = true;
        host['_'] = true;
        host['~'] = true;
        return host;
    }

    private static bool[] InitializeFieldValue()
    {
        // field-value https://tools.ietf.org/html/rfc7230#section-3.2
        bool[] fieldValue = new bool[TableSize];

        fieldValue[0x9] = true; // HTAB

        for (int c = 0x20; c <= 0x7e; c++) // VCHAR and SP
        {
            fieldValue[c] = true;
        }
        return fieldValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsInvalidAuthorityChar(Span<byte> s)
    {
        bool[] authority = s_authority;

        for (int i = 0; i < s.Length; i++)
        {
            byte c = s[i];
            if (c >= (uint)authority.Length || !authority[c])
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidHostChar(string s)
    {
        bool[] host = s_host;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= (uint)host.Length || !host[c])
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(string s)
    {
        bool[] token = s_token;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= (uint)token.Length || !token[c])
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(ReadOnlySpan<byte> span)
    {
        bool[] token = s_token;

        for (int i = 0; i < span.Length; i++)
        {
            byte c = span[i];
            if (c >= (uint)token.Length || !token[c])
            {
                return i;
            }
        }

        return -1;
    }

    // Follows field-value rules in https://tools.ietf.org/html/rfc7230#section-3.2
    // Disallows characters > 0x7E.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueChar(string s)
    {
        bool[] fieldValue = s_fieldValue;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= (uint)fieldValue.Length || !fieldValue[c])
            {
                return i;
            }
        }

        return -1;
    }

    // Follows field-value rules for chars <= 0x7F. Allows extended characters > 0x7F.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueCharExtended(string s)
    {
        bool[] fieldValue = s_fieldValue;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c < (uint)fieldValue.Length && !fieldValue[c])
            {
                return i;
            }
        }

        return -1;
    }
}
