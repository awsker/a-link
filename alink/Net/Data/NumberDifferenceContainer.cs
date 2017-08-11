using System;
using alink.Utils;

namespace alink.Net.Data
{
    public class NumberDifferenceContainer : IChangedDataContainer
    {
        private long _offset;
        private byte[] _difference;
        public NumberDifferenceContainer(byte[] difference, long offset)
        {
            _offset = offset;
            _difference = difference;
        }

        public byte[] GetNetworkBytes()
        {
            var size = sizeof (long) + 2 + _difference.Length;
            var bytes = new byte[size];
            bytes[0] = (byte) ChangeDataType.NumberDifference;
            int current = 1;
            ByteWriter.WriteLong(_offset, bytes, current);
            current += sizeof (long);
            bytes[current] = (byte) _difference.Length;
            current += 1;
            Array.Copy(_difference, 0, bytes, current, _difference.Length);
            return bytes;
        }

        public void FillFromNetworkBytes(byte[] bytes)
        {
            int current = 1;
            _offset = BitConverter.ToInt64(bytes, current);
            current += sizeof (long);
            var numBytes = (int)bytes[current];
            current += 1;
            _difference = new byte[numBytes];
            Array.Copy(bytes, current, _difference, 0, numBytes);
        }

        public void PushIntoProcessManager(ProcessManager manager, string username)
        {
            var ruleIndex = manager.FindRuleIndex(_offset);
            if (ruleIndex > -1)
            {
                manager.InjectDifferenceFromExternal(ruleIndex, _offset, _difference, username);   
            }
        }
    }
}
