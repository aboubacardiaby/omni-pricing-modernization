namespace Pricing.Compatibility.Cobol;

using System.Buffers.Binary;

public static class CobolCompCodec
{
    public static byte[] EncodeSigned(long value, int byteLength, CobolByteOrder byteOrder)
    {
        byte[] result = new byte[ValidateLength(byteLength)];
        switch (byteLength)
        {
            case 2 when value is >= short.MinValue and <= short.MaxValue:
                WriteInt16(result, (short)value, byteOrder);
                break;
            case 3 when value is >= -8_388_608 and <= 8_388_607:
                WriteInt24(result, (int)value, byteOrder);
                break;
            case 4 when value is >= int.MinValue and <= int.MaxValue:
                WriteInt32(result, (int)value, byteOrder);
                break;
            case 8:
                WriteInt64(result, value, byteOrder);
                break;
            default:
                throw new OverflowException($"Value {value} does not fit in a signed {byteLength}-byte COMP field.");
        }

        return result;
    }

    public static long DecodeSigned(ReadOnlySpan<byte> source, CobolByteOrder byteOrder) => source.Length switch
    {
        2 => byteOrder == CobolByteOrder.BigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(source)
            : BinaryPrimitives.ReadInt16LittleEndian(source),
        3 => ReadInt24(source, byteOrder),
        4 => byteOrder == CobolByteOrder.BigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(source)
            : BinaryPrimitives.ReadInt32LittleEndian(source),
        8 => byteOrder == CobolByteOrder.BigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(source)
            : BinaryPrimitives.ReadInt64LittleEndian(source),
        _ => throw new ArgumentException("A COMP field must be 2, 3, 4, or 8 bytes.", nameof(source)),
    };

    private static int ValidateLength(int byteLength) => byteLength is 2 or 3 or 4 or 8
        ? byteLength
        : throw new ArgumentOutOfRangeException(nameof(byteLength), "A COMP field must be 2, 3, 4, or 8 bytes.");

    private static void WriteInt24(Span<byte> destination, int value, CobolByteOrder order)
    {
        uint bits = unchecked((uint)value) & 0x00FF_FFFF;
        if (order == CobolByteOrder.BigEndian)
        {
            destination[0] = (byte)(bits >> 16);
            destination[1] = (byte)(bits >> 8);
            destination[2] = (byte)bits;
        }
        else
        {
            destination[0] = (byte)bits;
            destination[1] = (byte)(bits >> 8);
            destination[2] = (byte)(bits >> 16);
        }
    }

    private static int ReadInt24(ReadOnlySpan<byte> source, CobolByteOrder order)
    {
        int value = order == CobolByteOrder.BigEndian
            ? (source[0] << 16) | (source[1] << 8) | source[2]
            : source[0] | (source[1] << 8) | (source[2] << 16);
        return (value & 0x0080_0000) == 0 ? value : value | unchecked((int)0xFF00_0000);
    }

    private static void WriteInt16(Span<byte> destination, short value, CobolByteOrder order)
    {
        if (order == CobolByteOrder.BigEndian) BinaryPrimitives.WriteInt16BigEndian(destination, value);
        else BinaryPrimitives.WriteInt16LittleEndian(destination, value);
    }

    private static void WriteInt32(Span<byte> destination, int value, CobolByteOrder order)
    {
        if (order == CobolByteOrder.BigEndian) BinaryPrimitives.WriteInt32BigEndian(destination, value);
        else BinaryPrimitives.WriteInt32LittleEndian(destination, value);
    }

    private static void WriteInt64(Span<byte> destination, long value, CobolByteOrder order)
    {
        if (order == CobolByteOrder.BigEndian) BinaryPrimitives.WriteInt64BigEndian(destination, value);
        else BinaryPrimitives.WriteInt64LittleEndian(destination, value);
    }
}
