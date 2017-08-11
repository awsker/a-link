using System;
using alink.Models;

namespace alink.Utils
{
    public static class ByteParser
    {
        public static short ParseShort(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToInt16(bytes, 0);
        }

        public static ushort ParseUnsignedShort(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        public static int ParseInt(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        public static uint ParseUnsignedInt(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        public static long ParseLong(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToInt64(bytes, 0);
        }

        public static ulong ParseUnsignedLong(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        public static float ParseFloat(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToSingle(bytes, 0);
        }

        public static double ParseDouble(byte[] bytes, Endianness endian)
        {
            if (endian == Endianness.BigEndian)
                bytes = reverseArray(bytes);

            return BitConverter.ToDouble(bytes, 0);
        }

        private static byte[] reverseArray(byte[] bytes)
        {
            if (bytes.Length == 1)
                return bytes;
            var tempBytes = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                tempBytes[bytes.Length - 1 - i] = bytes[i];
            }
            return tempBytes;
        }

    }
}
