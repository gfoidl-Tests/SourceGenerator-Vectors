// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;

namespace Generator.Emitter;

internal static class HelpersEmitter
{
    public static void EmitHelpers(IndentedTextWriter writer)
    {
        writer.WriteLine();

        writer.WriteLine("[DebuggerNonUserCode]");

        // TODO: should be 'file class' but somehow this results in
        // "CS0116: A namespace cannot directly contain members such as fields, methods or statements"
        //writer.WriteLine("file class VectorHelper");
        writer.WriteLine("/* file */ internal class Core");
        writer.WriteLine("{");
        writer.Indent++;
        {
            EmitScalarMethods(writer);
            writer.WriteLine();
            EmitVector128HelperMethods(writer);
        }
        writer.Indent--;
        writer.WriteLine("}");

        writer.WriteLine();
    }
    //-------------------------------------------------------------------------
    private static void EmitScalarMethods(IndentedTextWriter writer)
    {
        // TODO: I don't know why that indentation is needed, but outupt shows it's OK so.
        const string Code = """
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int IndexOfMatchCharScalar<TNegator>(ReadOnlySpan<char> value, ReadOnlySpan<bool> lookup)
                    where TNegator : struct, INegator
                {
                    for (int i = 0; i < value.Length; ++i)
                    {
                        char c = value[i];

                        if (c >= lookup.Length || TNegator.NegateIfNeeded(!lookup[c]))
                        {
                            return i;
                        }
                    }

                    return -1;
                }
            """;

        writer.WriteLine(Code);
    }
    //-------------------------------------------------------------------------
    private static void EmitVector128HelperMethods(IndentedTextWriter writer)
    {
        // TODO: I don't know why that indentation is needed, but outupt shows it's OK so.
        const string Code = """
            public static int IndexOfMatchCharVectorized<TNegator>(ReadOnlySpan<char> value, Vector128<sbyte> bitMaskLookup)
                    where TNegator : struct, INegator
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
                        nuint end = (uint)(value.Length - 2 * Vector128<short>.Count);

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref ptr, idx);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref ptr, idx + 8);
                            Vector128<sbyte> values  = NarrowWithSaturation(source0, source1);

                            mask = CreateEscapingMask<TNegator>(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
                            if (mask != 0)
                            {
                                goto Found;
                            }

                            idx += 2 * (uint)Vector128<short>.Count;
                        }
                        while (idx <= end);
                    }

                    // Here we know that 8 to 15 chars are remaining. Process the first 8 chars.
                    if (idx <= (uint)(value.Length - Vector128<short>.Count))
                    {
                        Vector128<short> source = Vector128.LoadUnsafe(ref ptr, idx);
                        Vector128<sbyte> values = NarrowWithSaturation(source, source);

                        mask = CreateEscapingMask<TNegator>(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
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

                        mask = CreateEscapingMask<TNegator>(values, bitMaskLookup, bitPosLookup, nibbleMaskSByte, zeroMaskSByte);
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
                private static uint CreateEscapingMask<TNegator>(
                    Vector128<sbyte> values,
                    Vector128<sbyte> bitMaskLookup,
                    Vector128<sbyte> bitPosLookup,
                    Vector128<sbyte> nibbleMaskSByte,
                    Vector128<sbyte> nullMaskSByte)
                    where TNegator : struct, INegator
                {
                    // To check if an input byte matches or not, we use a bitmask-lookup.
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
                    // where 1 denotes a matach, while 0 means no match.
                    // For high-nibbles in the range 8..F every input needs to be escaped, so we
                    // can omit them in the bit-mask, thus only high-nibbles in the range 0..7 need
                    // to be considered, hence the entries in the bit-mask can be of type byte.
                    //
                    // In the bitmask-lookup for each row (= low-nibble) a bit-mask for the
                    // high-nibbles (= columns) is created.

                    Debug.Assert(Vector128.IsHardwareAccelerated);

                    // Perf: the shift needs to be done as Int32, as there exists a hw-instruction and no sw-emulation needs to be done.
                    // Cf. https://github.com/dotnet/runtime/issues/75770
                    // Due to the Int32-shift we need to mask out any remaining bits from the shifted nibble.
                    Vector128<sbyte> highNibbles = Vector128.ShiftRightLogical(values.AsInt32(), 4).AsSByte() & nibbleMaskSByte;
                    Vector128<sbyte> lowNibbles  = values & nibbleMaskSByte;

                    Vector128<sbyte> bitMask      = Shuffle(bitMaskLookup, lowNibbles);
                    Vector128<sbyte> bitPositions = Shuffle(bitPosLookup , highNibbles);

                    Vector128<sbyte> mask       = bitPositions & bitMask;
                    Vector128<sbyte> comparison = Vector128.Equals(nullMaskSByte, mask);
                    comparison                  = TNegator.NegateIfNeeded(comparison);

                    return comparison.ExtractMostSignificantBits();
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
                        // This is not the exact algorithm for saturation, but for the use-case
                        // here it's does what it should do. I.e. eliminate non-ASCII chars in the
                        // results.

                        Vector128<short> v0HighNibbles = Vector128.ShiftRightLogical(v0, 8);
                        Vector128<short> v1HighNibbles = Vector128.ShiftRightLogical(v1, 8);

                        Vector128<short> ascii0 = Vector128.Equals(Vector128<short>.Zero, v0HighNibbles);
                        Vector128<short> ascii1 = Vector128.Equals(Vector128<short>.Zero, v1HighNibbles);

                        v0 &= ascii0;
                        v1 &= ascii1;

                        return Vector128.Narrow(v0, v1);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static Vector128<sbyte> Shuffle(Vector128<sbyte> vector, Vector128<sbyte> indices)
                {
                    // Perf: Ssse3.Shuffle produces better code
                    return Sse3.IsSupported
                        ? Ssse3.Shuffle(vector, indices)
                        : Vector128.Shuffle(vector, indices);
                }

                public interface INegator
                {
                    static abstract bool NegateIfNeeded(bool equals);
                    static abstract Vector128<sbyte> NegateIfNeeded(Vector128<sbyte> equals);
                }

                public struct DontNegate : INegator
                {
                    public static bool NegateIfNeeded(bool equals) => equals;
                    public static Vector128<sbyte> NegateIfNeeded(Vector128<sbyte> equals) => equals;
                }

                public struct Negate : INegator
                {
                    public static bool NegateIfNeeded(bool equals) => !equals;
                    public static Vector128<sbyte> NegateIfNeeded(Vector128<sbyte> equals) => ~equals;
                }
            """;

        writer.WriteLine(Code);
    }
}
