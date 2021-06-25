using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal static class HttpCharacters_Vectorized
{
    private const int TableSize = 128;
    private static readonly BitMask s_alphaNumeric;
    private static readonly BitMask s_authority;
    private static readonly BitMask s_token;
    private static readonly BitMask s_host;
    private static readonly BitMask s_fieldValue;

    private static readonly Vector128<sbyte> s_bitMaskLookupAlphaNumeric;
    private static readonly Vector128<sbyte> s_bitMaskLookupAuthority;
    private static readonly Vector128<sbyte> s_bitMaskLookupToken;
    private static readonly Vector128<sbyte> s_bitMaskLookupHost;
    private static readonly Vector128<sbyte> s_bitMaskLookupFieldValue;

    static HttpCharacters_Vectorized()
    {
        (s_alphaNumeric, s_bitMaskLookupAlphaNumeric) = InitializeAlphaNumeric();
        (s_authority, s_bitMaskLookupAuthority) = InitializeAuthority();
        (s_token, s_bitMaskLookupToken) = InitializeToken();
        (s_host, s_bitMaskLookupHost) = InitializeHost();
        (s_fieldValue, s_bitMaskLookupFieldValue) = InitializeFieldValue();
    }

    [ModuleInitializer]
    internal static void Init()
    {
        _ = s_alphaNumeric;
        _ = s_authority;
        _ = s_token;
        _ = s_host;
        _ = s_fieldValue;


        _ = s_bitMaskLookupAlphaNumeric;
        _ = s_bitMaskLookupAuthority;
        _ = s_bitMaskLookupToken;
        _ = s_bitMaskLookupHost;
        _ = s_bitMaskLookupFieldValue;
    }

    private unsafe static void SetBitInMask(sbyte* mask, int c)
    {
        Debug.Assert(c < 128);

        int highNibble = c >> 4;
        int lowNibble = c & 0xF;

        mask[lowNibble] &= (sbyte)(~(1 << highNibble));
    }

    private static unsafe (BitMask, Vector128<sbyte>) InitializeAlphaNumeric()
    {
        // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1

        BitMask alphaNumeric = BitMask.Create();
        Vector128<sbyte> vector = Vector128.Create((sbyte)-1);
        sbyte* mask = (sbyte*)&vector;

        SetMask('0', '9');
        SetMask('A', 'Z');
        SetMask('a', 'z');

        void SetMask(char first, char last)
        {
            for (char c = first; c <= last; ++c)
            {
                alphaNumeric.SetValid(c);
                SetBitInMask(mask, c);
            }
        }

        return (alphaNumeric, vector);
    }

    private static unsafe (BitMask, Vector128<sbyte>) InitializeAuthority()
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

        BitMask authority = BitMask.Create(s_alphaNumeric);
        Vector128<sbyte> vector = s_bitMaskLookupAlphaNumeric;
        sbyte* mask = (sbyte*)&vector;

        foreach (char c in ":.[]@")
        {
            authority.SetValid(c);
            SetBitInMask(mask, c);
        }

        return (authority, vector);
    }

    private static unsafe (BitMask, Vector128<sbyte>) InitializeToken()
    {
        // tchar https://tools.ietf.org/html/rfc7230#appendix-B

        BitMask token = BitMask.Create(s_alphaNumeric);
        Vector128<sbyte> vector = s_bitMaskLookupAlphaNumeric;
        sbyte* mask = (sbyte*)&vector;

        foreach (char c in "!#$%&\'*+-.^_`|~")
        {
            token.SetValid(c);
            SetBitInMask(mask, c);
        }

        return (token, vector);
    }

    private static unsafe (BitMask, Vector128<sbyte>) InitializeHost()
    {
        // Matches Http.Sys
        // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys

        BitMask host = BitMask.Create(s_alphaNumeric);
        Vector128<sbyte> vector = s_bitMaskLookupAlphaNumeric;
        sbyte* mask = (sbyte*)&vector;

        foreach (char c in "!$&\'()-._~")
        {
            host.SetValid(c);
            SetBitInMask(mask, c);
        }

        return (host, vector);
    }

    private static unsafe (BitMask, Vector128<sbyte>) InitializeFieldValue()
    {
        // field-value https://tools.ietf.org/html/rfc7230#section-3.2

        BitMask fieldValue = BitMask.Create();
        Vector128<sbyte> vector = Vector128.Create((sbyte)-1);
        sbyte* mask = (sbyte*)&vector;

        for (char c = (char)0x20; c <= (char)0x7e; c++) // VCHAR and SP
        {
            fieldValue.SetValid(c);
            SetBitInMask(mask, c);
        }

        return (fieldValue, vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int IndexOfInvalidHostChar(string s)
    {
        fixed (char* ptr = s)
        {
            nint idx = 0;
            nint n = (nint)(uint)s.Length;

            if (Ssse3.IsSupported && n - 8 >= idx)
            {
                Vector128<sbyte> bitMaskLookup = s_bitMaskLookupHost;

                // To check if a bit in a bitmask from the Bitmask is set, in a sequential code
                // we would do ((1 << bitIndex) & bitmask) != 0
                // As there is no hardware instrinic for such a shift, we use a lookup that
                // stores the shifted bitpositions.
                // So (1 << bitIndex) becomes BitPosLook[bitIndex], which is simd-friendly.
                //
                // A bitmask from the Bitmask (above) is created only for values 0..7 (one byte),
                // so to avoid a explicit check for values outside 0..7, i.e.
                // high nibbles 8..F, we use a bitpos that always results in escaping.
                Vector128<sbyte> bitPosLookup = Vector128.Create(
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,     // high-nibble 0..7
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF      // high-nibble 8..F
                ).AsSByte();

                Vector128<sbyte> nibbleMaskSByte = Vector128.Create((sbyte)0xF);
                Vector128<sbyte> nullMaskSByte = Vector128<sbyte>.Zero;

                do
                {
                    Debug.Assert(idx <= (n - 8));

                    Vector128<short> source = Sse2.LoadVector128((short*)(ptr + idx));
                    Vector128<sbyte> values = Sse2.PackSignedSaturate(source, source);

                    Vector128<sbyte> mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
                    int index = Sse2.MoveMask(mask);

                    if (index != 0)
                    {
                        idx += (nint)(uint)BitHelper.GetIndexOfFirstNeedToEscape(index);
                        return (int)idx;
                    }

                    idx += 8;
                } while (n - 8 >= idx);
            }

            if (idx < n)
            {
                BitMask host = s_host;

                do
                {
                    Debug.Assert((ptr + idx) <= (ptr + s.Length));

                    char c = ptr[idx];
                    if (c >= TableSize || !host.IsValid(c))
                    {
                        return (int)idx;
                    }
                } while (++idx < n);
            }
        }

        return -1;
    }

    private unsafe struct BitMask
    {
        private fixed bool _data[TableSize];

        public static BitMask Create() => new BitMask();

        public static BitMask Create(BitMask other)
        {
            BitMask bitmask = new();
            Buffer.MemoryCopy(other._data, bitmask._data, TableSize, TableSize);

            return bitmask;
        }

        public void SetValid(char c)
        {
            Debug.Assert(c < 128);
            _data[c] = true;
        }

        public bool IsValid(char c)
        {
            Debug.Assert(c < 128);
            return _data[(uint)c];
        }
    }

    internal static class BitHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexOfFirstNeedToEscape(int index)
        {
            // Found at least one byte that needs to be escaped, figure out the index of
            // the first one found that needed to be escaped within the 16 bytes.
            Debug.Assert(index > 0 && index <= 65_535);
            int tzc = BitOperations.TrailingZeroCount(index);
            Debug.Assert(tzc >= 0 && tzc < 16);

            return tzc;
        }
    }

    internal static class Ssse3Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask(
            Vector128<sbyte> values,
            Vector128<sbyte> bitMaskLookup,
            Vector128<sbyte> bitPosLookup,
            Vector128<sbyte> nibbleMaskSByte,
            Vector128<sbyte> nullMaskSByte)
        {
            // To check if an input byte needs to be escaped or not, we use a bitmask-lookup.
            // Therefore we split the input byte into the low- and high-nibble, which will get
            // the row-/column-index in the bit-mask.
            // The bitmask-lookup looks like (here for example s_bitMaskLookupBasicLatin):
            //                                     high-nibble
            // low-nibble  0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
            //         0   1   1   0   0   0   0   1   0   1   1   1   1   1   1   1   1
            //         1   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         2   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         3   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         4   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         5   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         6   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         7   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         8   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         9   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         A   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         B   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         C   1   1   0   1   0   1   0   0   1   1   1   1   1   1   1   1
            //         D   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         E   1   1   0   1   0   0   0   0   1   1   1   1   1   1   1   1
            //         F   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1   1
            //
            // where 1 denotes the neeed for escaping, while 0 means no escaping needed.
            // For high-nibbles in the range 8..F every input needs to be escaped, so we
            // can omit them in the bit-mask, thus only high-nibbles in the range 0..7 need
            // to be considered, hence the entries in the bit-mask can be of type byte.
            //
            // In the bitmask-lookup for each row (= low-nibble) a bit-mask for the
            // high-nibbles (= columns) is created.

            Debug.Assert(Ssse3.IsSupported);

            Vector128<sbyte> highNibbles = Sse2.And(Sse2.ShiftRightLogical(values.AsInt32(), 4).AsSByte(), nibbleMaskSByte);
            Vector128<sbyte> lowNibbles = Sse2.And(values, nibbleMaskSByte);

            Vector128<sbyte> bitMask = Ssse3.Shuffle(bitMaskLookup, lowNibbles);
            Vector128<sbyte> bitPositions = Ssse3.Shuffle(bitPosLookup, highNibbles);

            Vector128<sbyte> mask = Sse2.And(bitPositions, bitMask);

            mask = Sse2.CompareEqual(nullMaskSByte, Sse2.CompareEqual(nullMaskSByte, mask));
            return mask;
        }
    }
}
