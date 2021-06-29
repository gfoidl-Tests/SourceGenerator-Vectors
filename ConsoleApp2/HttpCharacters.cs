// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        var alphaNumeric = new bool[TableSize];
        for (var c = '0'; c <= '9'; c++)
        {
            alphaNumeric[c] = true;
        }
        for (var c = 'A'; c <= 'Z'; c++)
        {
            alphaNumeric[c] = true;
        }
        for (var c = 'a'; c <= 'z'; c++)
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
        var authority = new bool[TableSize];
        Array.Copy(s_alphaNumeric, authority, TableSize);
        authority[':'] = true;
        authority['.'] = true;
        authority['['] = true;
        authority[']'] = true;
        authority['@'] = true;
        return authority;
    }

    private static bool[] InitializeToken()
    {
        // tchar https://tools.ietf.org/html/rfc7230#appendix-B
        var token = new bool[TableSize];
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
        var host = new bool[TableSize];
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
        var fieldValue = new bool[TableSize];
        for (var c = 0x20; c <= 0x7e; c++) // VCHAR and SP
        {
            fieldValue[c] = true;
        }
        return fieldValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsInvalidAuthorityChar(Span<byte> s)
    {
        var authority = s_authority;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
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
        var host = s_host;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
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
        var token = s_token;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
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
        var token = s_token;

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c >= (uint)token.Length || !token[c])
            {
                return i;
            }
        }

        return -1;
    }

    // Disallows control characters and anything more than 0x7E
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueChar(string s)
    {
        var fieldValue = s_fieldValue;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c >= (uint)fieldValue.Length || !fieldValue[c])
            {
                return i;
            }
        }

        return -1;
    }

    // Disallows control characters but allows extended characters > 0x7F
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueCharExtended(string s)
    {
        var fieldValue = s_fieldValue;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c < (uint)fieldValue.Length && !fieldValue[c])
            {
                return i;
            }
        }

        return -1;
    }
}
