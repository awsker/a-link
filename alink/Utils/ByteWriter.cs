using System;

namespace alink.Utils
{
    public static class ByteWriter
    {

        public static void WriteUShort(ushort number, byte[] destination, int offset)
        {
            destination[offset + 1] = (byte)(number >> 8);
            destination[offset] = (byte)(number & 255);
        }

        public static void WriteUInt(uint number, byte[] destination, int offset)
        {
            var bytes = BitConverter.GetBytes(number);
            destination[offset] = bytes[0];
            destination[offset + 1] = bytes[1];
            destination[offset + 2] = bytes[2];
            destination[offset + 3] = bytes[3];
        }

        public static void WriteLong(long number, byte[] destination, int offset)
        {
            var bytes = BitConverter.GetBytes(number);
            destination[offset] = bytes[0];
            destination[offset + 1] = bytes[1];
            destination[offset + 2] = bytes[2];
            destination[offset + 3] = bytes[3];
            destination[offset + 4] = bytes[4];
            destination[offset + 5] = bytes[5];
            destination[offset + 6] = bytes[6];
            destination[offset + 7] = bytes[7];
        }

        public static void WriteULong(ulong number, byte[] destination, int offset)
        {
            var bytes = BitConverter.GetBytes(number);
            destination[offset] = bytes[0];
            destination[offset + 1] = bytes[1];
            destination[offset + 2] = bytes[2];
            destination[offset + 3] = bytes[3];
            destination[offset + 4] = bytes[4];
            destination[offset + 5] = bytes[5];
            destination[offset + 6] = bytes[6];
            destination[offset + 7] = bytes[7];
        }
    }
}
