using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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
        fixed (byte* ptr = s)
        {
            int index = Ssse3.IsSupported && s.Length >= Vector128<byte>.Count
                ? IndexOfInvalidCharVectorized(ptr, (nint)(uint)s.Length, BitMaskLookupAuthority())
                : IndexOfInvalidCharScalar(ptr, (nint)(uint)s.Length, LookupAuthority());

            return index >= 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidHostChar(string s)
    {
        fixed (char* ptr = s)
        {
            return Ssse3.IsSupported && s.Length >= Vector128<short>.Count
                ? IndexOfInvalidCharVectorized(ptr, (nint)(uint)s.Length, BitMaskLookupHost())
                : IndexOfInvalidCharScalar(ptr, (nint)(uint)s.Length, LookupHost());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(string s)
    {
        fixed (char* ptr = s)
        {
            return Ssse3.IsSupported && s.Length >= Vector128<short>.Count
                ? IndexOfInvalidCharVectorized(ptr, (nint)(uint)s.Length, BitMaskLookupToken())
                : IndexOfInvalidCharScalar(ptr, (nint)(uint)s.Length, LookupToken());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidTokenChar(ReadOnlySpan<byte> span)
    {
        fixed (byte* ptr = span)
        {
            return Ssse3.IsSupported && span.Length >= Vector128<byte>.Count
                ? IndexOfInvalidCharVectorized(ptr, (nint)(uint)span.Length, BitMaskLookupToken())
                : IndexOfInvalidCharScalar(ptr, (nint)(uint)span.Length, LookupToken());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueChar(string s)
    {
        fixed (char* ptr = s)
        {
            return Ssse3.IsSupported && s.Length >= Vector128<short>.Count
                ? IndexOfInvalidCharVectorized(ptr, (nint)(uint)s.Length, BitMaskLookupFieldValue())
                : IndexOfInvalidCharScalar(ptr, (nint)(uint)s.Length, LookupFieldValue());
        }
    }

    // Disallows control characters but allows extended characters > 0x7F
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfInvalidFieldValueCharExtended(string s)
    {
        // This is to workaround  https://github.com/dotnet/runtime/issues/12241
        ReadOnlySpan<bool> fieldValue = LookupFieldValue();
        ref bool lookupRef = ref MemoryMarshal.GetReference(fieldValue);

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (c < (uint)fieldValue.Length && !Unsafe.Add(ref lookupRef, (uint)c))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharScalar(char* ptr, nint length, ReadOnlySpan<bool> lookup)
    {
        // This is to workaround  https://github.com/dotnet/runtime/issues/12241
        ref bool lookupRef = ref MemoryMarshal.GetReference(lookup);

        for (nint i = 0; i < length; ++i)
        {
            char c = ptr[i];

            if (c >= lookup.Length || !Unsafe.Add(ref lookupRef, (uint)c))
            {
                return (int)i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharScalar(byte* ptr, nint length, ReadOnlySpan<bool> lookup)
    {
        // This is to workaround  https://github.com/dotnet/runtime/issues/12241
        ref bool lookupRef = ref MemoryMarshal.GetReference(lookup);

        for (nint i = 0; i < length; ++i)
        {
            byte b = ptr[i];

            if (b >= lookup.Length || !Unsafe.Add(ref lookupRef, (uint)b))
            {
                return (int)i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharVectorized(char* ptr, nint length, Vector128<sbyte> bitMaskLookup)
    {
        Debug.Assert(Ssse3.IsSupported);
        Debug.Assert(length >= Vector128<short>.Count);

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
        Vector128<sbyte> nullMaskSByte = Vector128<sbyte>.Zero;

        nint idx = 0;
        int mask;

        while (length - 2 * Vector128<short>.Count >= idx)
        {
            Vector128<short> source0 = Sse2.LoadVector128((short*)(ptr + idx));
            Vector128<short> source1 = Sse2.LoadVector128((short*)(ptr + idx + 8));
            Vector128<sbyte> values = Sse2.PackSignedSaturate(source0, source1);

            mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
            if (mask != 0)
            {
                goto Found;
            }

            idx += 2 * Vector128<short>.Count;
        }

        // Here we know that 8 to 15 chars are remaining. Process the first 8 chars.
        if (length - Vector128<short>.Count >= idx)
        {
            Vector128<short> source = Sse2.LoadVector128((short*)(ptr + idx));
            Vector128<sbyte> values = Sse2.PackSignedSaturate(source, source);

            mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
            if (mask != 0)
            {
                goto Found;
            }

            idx += Vector128<short>.Count;
        }

        // Here we know that < 8 chars are remaining. We shift the space around to process
        // another full vector.
        nint remaining = length - idx;
        if (remaining > 0)
        {
            remaining -= Vector128<short>.Count;

            Vector128<short> source = Sse2.LoadVector128((short*)(ptr + idx + remaining));
            Vector128<sbyte> values = Sse2.PackSignedSaturate(source, source);

            mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
            if (mask != 0)
            {
                idx += remaining;
                goto Found;
            }
        }

        goto NotFound;

    Found:
        idx += (nint)(uint)BitHelper.GetIndexOfFirstNeedToEscape(mask);
        return (int)idx;

    NotFound:
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidCharVectorized(byte* ptr, nint length, Vector128<sbyte> bitMaskLookup)
    {
        Debug.Assert(Ssse3.IsSupported);
        Debug.Assert(length >= Vector128<byte>.Count);

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

        nint idx = 0;
        int mask;

        while (length - Vector128<byte>.Count >= idx)
        {
            Vector128<sbyte> values = Sse2.LoadVector128((sbyte*)(ptr + idx));

            mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
            if (mask != 0)
            {
                goto Found;
            }

            idx += Vector128<byte>.Count;
        }

        // Here we know that < 16 bytes are remaining. We shift the space around to process
        // another full vector.
        nint remaining = length - idx;
        if (remaining > 0)
        {
            remaining -= Vector128<byte>.Count;

            Vector128<sbyte> values = Sse2.LoadVector128((sbyte*)ptr + idx + remaining);

            mask = Ssse3Helper.CreateEscapingMask(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
            if (mask != 0)
            {
                idx += remaining;
                goto Found;
            }
        }

        goto NotFound;

    Found:
        idx += (nint)(uint)BitHelper.GetIndexOfFirstNeedToEscape(mask);
        return (int)idx;

    NotFound:
        return -1;
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
        public static int CreateEscapingMask(
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

            Debug.Assert(Ssse3.IsSupported);

            Vector128<sbyte> highNibbles = Sse2.And(Sse2.ShiftRightLogical(values.AsInt32(), 4).AsSByte(), nibbleMaskSByte);
            Vector128<sbyte> lowNibbles = Sse2.And(values, nibbleMaskSByte);

            Vector128<sbyte> bitMask = Ssse3.Shuffle(bitMaskLookup, lowNibbles);
            Vector128<sbyte> bitPositions = Ssse3.Shuffle(bitPosLookup, highNibbles);

            Vector128<sbyte> mask = Sse2.And(bitPositions, bitMask);

            Vector128<sbyte> comparison = Sse2.CompareEqual(nullMaskSByte, Sse2.CompareEqual(nullMaskSByte, mask));
            return Sse2.MoveMask(comparison);
        }
    }
}
