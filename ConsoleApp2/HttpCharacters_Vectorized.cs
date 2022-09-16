﻿using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

internal static unsafe partial class HttpCharacters_Vectorized
{
    private static partial ReadOnlySpan<bool> LookupAlphaNumeric();
    private static partial ReadOnlySpan<bool> LookupAuthority();
    private static partial ReadOnlySpan<bool> LookupToken();
    private static partial ReadOnlySpan<bool> LookupHost();
    private static partial ReadOnlySpan<bool> LookupFieldValue();

    private static partial Vector128<sbyte> BitmaskAlphaNumeric();
    private static partial Vector128<sbyte> BitMaskLookupAuthority();
    private static partial Vector128<sbyte> BitMaskLookupToken();
    private static partial Vector128<sbyte> BitMaskLookupHost();
    private static partial Vector128<sbyte> BitMaskLookupFieldValue();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsInvalidAuthorityChar(Span<byte> s)
    {
        int index = Vector128.IsHardwareAccelerated && s.Length >= Vector128<byte>.Count
            ? IndexOfInvalidCharVectorized(s, BitMaskLookupAuthority())
            : IndexOfInvalidCharScalar    (s, LookupAuthority());

        return index >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidHostChar(string s)
    {
        return Vector128.IsHardwareAccelerated && s.Length >= Vector128<short>.Count
            ? IndexOfInvalidCharVectorized(s, BitMaskLookupHost())
            : IndexOfInvalidCharScalar    (s, LookupHost());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(string s)
    {
        return Vector128.IsHardwareAccelerated && s.Length >= Vector128<short>.Count
            ? IndexOfInvalidCharVectorized(s, BitMaskLookupToken())
            : IndexOfInvalidCharScalar    (s, LookupToken());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(ReadOnlySpan<byte> s)
    {
        return Vector128.IsHardwareAccelerated && s.Length >= Vector128<byte>.Count
            ? IndexOfInvalidCharVectorized(s, BitMaskLookupToken())
            : IndexOfInvalidCharScalar    (s, LookupToken());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueChar(string s)
    {
        return Vector128.IsHardwareAccelerated && s.Length >= Vector128<short>.Count
            ? IndexOfInvalidCharVectorized(s, BitMaskLookupFieldValue())
            : IndexOfInvalidCharScalar    (s, LookupFieldValue());
    }

    // Disallows control characters but allows extended characters > 0x7F
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueCharExtended(string s)
    {
        return Vector128.IsHardwareAccelerated && s.Length >= Vector128<short>.Count
            ? IndexOfInvalidCharExtendedVectorized(s, BitMaskLookupFieldValue())
            : IndexOfInvalidCharExtendedScalar    (s, LookupFieldValue());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharScalar(ReadOnlySpan<char> value, ReadOnlySpan<bool> lookup)
    {
        for (int i = 0; i < value.Length; ++i)
        {
            char c = value[i];

            if (c >= lookup.Length || !lookup[c])
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharScalar(ReadOnlySpan<byte> value, ReadOnlySpan<bool> lookup)
    {
        for (int i = 0; i < value.Length; ++i)
        {
            byte b = value[i];

            if (b >= lookup.Length || !lookup[b])
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharExtendedScalar(ReadOnlySpan<char> value, ReadOnlySpan<bool> lookup)
    {
        for (int i = 0; i < value.Length; ++i)
        {
            char c = value[i];

            if (c < (uint)lookup.Length && !lookup[c])
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharVectorized(ReadOnlySpan<char> value, Vector128<sbyte> bitMaskLookup)
    {
        Debug.Assert(Vector128.IsHardwareAccelerated);
        Debug.Assert(value.Length >= Vector128<short>.Count);

        // To check if a bit in a bitmask from the Bitmask is set, in a sequential code
        // we would do ((1 << bitIndex) & bitmask) != 0
        // As there is no hardware instrinic for such a shift, we use a lookup that
        // stores the shifted bitpositions.
        // So (1 << bitIndex) becomes BitPosLook[bitIndex], which is simd-friendly.
        //
        // A bitmask from the bitMaskLookup is created only for values 0..7 (one byte),
        // so to avoid a explicit check for values outside 0..7, i.e.
        // high nibbles 8..F, we use a bitpos that always results in escaping.
        Vector128<sbyte> bitPosLookup = Vector128.Create(
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,     // high-nibble 0..7
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF      // high-nibble 8..F
        ).AsSByte();

        Vector128<sbyte> nibbleMaskSByte = Vector128.Create((sbyte)0xF);
        Vector128<sbyte> zeroMaskSByte   = Vector128<sbyte>.Zero;

        nuint idx = 0;
        uint mask;
        ref short ptr = ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(value));

        if (value.Length >= 2 * Vector128<short>.Count)
        {
            do
            {
                Vector128<short> source0 = Vector128.LoadUnsafe(ref ptr, idx);
                Vector128<short> source1 = Vector128.LoadUnsafe(ref ptr, idx + 8);
                Vector128<sbyte> values  = NarrowWithSaturation(source0, source1);

                mask = CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
                if (mask != 0)
                {
                    goto Found;
                }

                idx += 2 * (uint)Vector128<short>.Count;
            }
            while (idx <= (uint)(value.Length - 2 * Vector128<short>.Count));
        }

        // Here we know that 8 to 15 chars are remaining. Process the first 8 chars.
        if (idx <= (uint)(value.Length - Vector128<short>.Count))
        {
            Vector128<short> source = Vector128.LoadUnsafe(ref ptr, idx);
            Vector128<sbyte> values = NarrowWithSaturation(source, source);

            mask = CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
            if (mask != 0)
            {
                goto Found;
            }

            idx += (uint)Vector128<short>.Count;
        }

        // Here we know that < 8 chars are remaining. We shift the space around to process
        // another full vector.
        nuint remaining = (uint)value.Length - idx;
        if ((nint)remaining > 0)
        {
            remaining -= (uint)Vector128<short>.Count;

            Vector128<short> source = Vector128.LoadUnsafe(ref ptr, idx + remaining);
            Vector128<sbyte> values = NarrowWithSaturation(source, source);

            mask = CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
            if (mask != 0)
            {
                idx += remaining;
                goto Found;
            }
        }

        goto NotFound;

    Found:
        idx += GetIndexOfFirstNeedToEscape(mask);
        return (int)idx;

    NotFound:
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharVectorized(ReadOnlySpan<byte> value, Vector128<sbyte> bitMaskLookup)
    {
        Debug.Assert(Vector128.IsHardwareAccelerated);
        Debug.Assert(value.Length >= Vector128<byte>.Count);

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
        Vector128<sbyte> zeroMaskSByte   = Vector128<sbyte>.Zero;

        nuint idx = 0;
        uint mask;
        ref byte ptr = ref MemoryMarshal.GetReference(value);

        while (idx <= (uint)(value.Length - Vector128<byte>.Count))
        {
            Vector128<sbyte> values = Vector128.LoadUnsafe(ref ptr, idx).AsSByte();

            mask = CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
            if (mask != 0)
            {
                goto Found;
            }

            idx += (uint)Vector128<byte>.Count;
        }

        // Here we know that < 16 bytes are remaining. We shift the space around to process
        // another full vector.
        nuint remaining = (uint)value.Length - idx;
        if ((nint)remaining > 0)
        {
            remaining -= (uint)Vector128<byte>.Count;

            Vector128<sbyte> values = Vector128.LoadUnsafe(ref ptr, idx + remaining).AsSByte();

            mask = CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
            if (mask != 0)
            {
                idx += remaining;
                goto Found;
            }
        }

        goto NotFound;

    Found:
        idx += GetIndexOfFirstNeedToEscape(mask);
        return (int)idx;

    NotFound:
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharExtendedVectorized(ReadOnlySpan<char> value, Vector128<sbyte> bitMaskLookup)
    {
        Debug.Assert(Vector128.IsHardwareAccelerated);
        Debug.Assert(value.Length >= Vector128<short>.Count);

        // To check if a bit in a bitmask from the Bitmask is set, in a sequential code
        // we would do ((1 << bitIndex) & bitmask) != 0
        // As there is no hardware instrinic for such a shift, we use a lookup that
        // stores the shifted bitpositions.
        // So (1 << bitIndex) becomes BitPosLook[bitIndex], which is simd-friendly.
        //
        // A bitmask from the bitMaskLookup is created only for values 0..7 (one byte),
        // so to avoid a explicit check for values outside 0..7, i.e.
        // high nibbles 8..F, we use a bitpos that always results in escaping.
        Vector128<sbyte> bitPosLookup = Vector128.Create(
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,     // high-nibble 0..7
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF      // high-nibble 8..F
        ).AsSByte();

        Vector128<sbyte> nibbleMaskSByte   = Vector128.Create((sbyte)0xF);
        Vector128<sbyte> zeroMaskSByte     = Vector128<sbyte>.Zero;
        Vector128<short> nonAsciiMaskShort = Vector128.Create((ushort)0xFF80).AsInt16();

        nuint idx = 0;
        uint mask;
        ref short ptr = ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(value));

        // Perf: don't unroll here, as it won't inline anymore due too excessive use of vector-registers.
        while (idx <= (uint)(value.Length - Vector128<short>.Count))
        {
            Vector128<short> source    = Vector128.LoadUnsafe(ref ptr, idx);
            Vector128<sbyte> values    = Vector128.Narrow(source, source);
            Vector128<sbyte> asciiMask = CreateAsciiMask(nonAsciiMaskShort, zeroMaskSByte, source);

            mask = CreateEscapingMaskExtended(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte, asciiMask);
            if (mask != 0)
            {
                goto Found;
            }

            idx += (uint)Vector128<short>.Count;
        }

        // Here we know that < 8 chars are remaining. We shift the space around to process
        // another full vector.
        nuint remaining = (uint)value.Length - idx;
        if ((nint)remaining > 0)
        {
            remaining -= (uint)Vector128<short>.Count;

            Vector128<short> source    = Vector128.LoadUnsafe(ref ptr, idx + remaining);
            Vector128<sbyte> values    = Vector128.Narrow(source, source);
            Vector128<sbyte> asciiMask = CreateAsciiMask(nonAsciiMaskShort, zeroMaskSByte, source);

            mask = CreateEscapingMaskExtended(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte, asciiMask);
            if (mask != 0)
            {
                idx += remaining;
                goto Found;
            }
        }

        goto NotFound;

    Found:
        idx += GetIndexOfFirstNeedToEscape(mask);
        return (int)idx;

    NotFound:
        return -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector128<sbyte> CreateAsciiMask(Vector128<short> nonAsciiMask, Vector128<sbyte> zero, Vector128<short> values)
        {
            Vector128<short> masked = values & nonAsciiMask;
            Vector128<short> ascii  = Vector128.Equals(masked, zero.AsInt16());
            return Vector128.Narrow(ascii, ascii);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint CreateEscapingMaskExtended(
            Vector128<sbyte> values,
            Vector128<sbyte> bitMaskLookup,
            Vector128<sbyte> bitPosLookup,
            Vector128<sbyte> nibbleMaskSByte,
            Vector128<sbyte> nullMaskSByte,
            Vector128<sbyte> asciiMask)
        {
            Debug.Assert(Vector128.IsHardwareAccelerated);

            Vector128<sbyte> highNibbles = Vector128.ShiftRightLogical(values, 4);
            Vector128<sbyte> lowNibbles  = values & nibbleMaskSByte;

            Vector128<sbyte> bitMask      = Vector128.Shuffle(bitMaskLookup, lowNibbles);
            Vector128<sbyte> bitPositions = Vector128.Shuffle(bitPosLookup, highNibbles);

            Vector128<sbyte> mask = bitPositions & bitMask;

            Vector128<sbyte> comparison = Vector128.Equals(mask      , nullMaskSByte);
            comparison                  = Vector128.Equals(comparison, nullMaskSByte);
            comparison &= asciiMask;

            return comparison.ExtractMostSignificantBits();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<sbyte> NarrowWithSaturation(Vector128<short> v0, Vector128<short> v1)
    {
        Debug.Assert(Vector128.IsHardwareAccelerated);

        // TODO: https://github.com/dotnet/runtime/issues/75724

        if (Sse2.IsSupported)
        {
            return Sse2.PackSignedSaturate(v0, v1);
        }
        else
        {
            Vector128<ushort> nonAsciiMask = Vector128.Create((ushort)0x7F);

            Vector128<ushort> nonAscii0 = Vector128.GreaterThan(v0.AsUInt16(), nonAsciiMask);
            v0 = Vector128.ConditionalSelect(nonAscii0, nonAsciiMask, v0.AsUInt16()).AsInt16();

            Vector128<ushort> nonAscii1 = Vector128.GreaterThan(v1.AsUInt16(), nonAsciiMask);
            v1 = Vector128.ConditionalSelect(nonAscii1, nonAsciiMask, v1.AsUInt16()).AsInt16();

            return Vector128.Narrow(v0, v1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetIndexOfFirstNeedToEscape(uint mask)
    {
        // Found at least one byte that needs to be escaped, figure out the index of
        // the first one found that needs to be escaped within the 16 bytes.
        Debug.Assert(mask > 0 && mask <= 65_535);
        uint tzc = uint.TrailingZeroCount(mask);
        Debug.Assert(tzc < 16);

        return tzc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CreateEscapingMask(
        Vector128<sbyte> values,
        Vector128<sbyte> bitMaskLookup,
        Vector128<sbyte> bitPosLookup,
        Vector128<sbyte> nibbleMaskSByte,
        Vector128<sbyte> nullMaskSByte)
    {
        // To check if an input byte needs to be escaped or not, we use a bitmask-lookup.
        // Therefore we split the input byte into the low- and high-nibble, which will get
        // the row-/column-index in the bit-mask.
        // The bitmask-lookup looks like:
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

        Debug.Assert(Vector128.IsHardwareAccelerated);

        Vector128<sbyte> highNibbles = Vector128.ShiftRightLogical(values, 4);
        Vector128<sbyte> lowNibbles  = values & nibbleMaskSByte;

        Vector128<sbyte> bitMask      = Vector128.Shuffle(bitMaskLookup, lowNibbles);
        Vector128<sbyte> bitPositions = Vector128.Shuffle(bitPosLookup , highNibbles);

        Vector128<sbyte> mask = bitPositions & bitMask;

        Vector128<sbyte> comparison = Vector128.Equals(nullMaskSByte, mask);
        comparison = Vector128.Equals(nullMaskSByte, comparison);

        return comparison.ExtractMostSignificantBits();
    }
}
