using System;
using System.Collections.Generic;
using System.Linq;
using alink.Models;
using alink.Utils;

namespace alink.Net.Data
{
    public class BytesDifferenceContainer : List<ChangedBytesRange>, IChangedDataContainer
    {
        private long _offset;

        public BytesDifferenceContainer(long ruleOffset)
        {
            _offset = ruleOffset;
        }

        public void FillFromNetworkBytes(byte[] bytes)
        {
            int current = 1;
            Clear();
            _offset = BitConverter.ToInt64(bytes, current);
            current += sizeof (long);
            var count = BitConverter.ToUInt16(bytes, current);
            current += sizeof (ushort);

            while (count-- > 0)
            {
                long offset = BitConverter.ToUInt32(bytes, current);
                current += sizeof (uint);
                ushort numBytes = BitConverter.ToUInt16(bytes, current);
                current += sizeof (ushort);
                var subBytes = new byte[numBytes];
                Array.Copy(bytes, current, subBytes, 0, numBytes);
                current += numBytes;
                Add(new ChangedBytesRange(subBytes, offset));
            }
        }

        public byte[] GetNetworkBytes()
        {
            //Rule base offset + Number of parts + Each Part with number of bytes and offset from base + all the bytes
            var totalForOptimizedPacket = 1 + sizeof(long) + sizeof(ushort) + Count * (sizeof(ushort)+sizeof(uint)) + this.Sum(range => range.Bytes.Length);
            var bytes = new byte[totalForOptimizedPacket];
            int current = 1;
            bytes[0] = (byte) ChangeDataType.BytesDifference;
            ByteWriter.WriteLong(_offset, bytes, current);
            current += sizeof (long);
            ByteWriter.WriteUShort((ushort) Count, bytes, current);
            current += sizeof(ushort);
            foreach (var range in this)
            {
                //Cast to uint. Hopefully the offsets won't be longer than that
                ByteWriter.WriteUInt((uint)range.Offset, bytes, current);
                current += sizeof(uint);
                ByteWriter.WriteUShort((ushort) range.Bytes.Length, bytes, current);
                current += sizeof (ushort);
                Array.Copy(range.Bytes, 0, bytes, current, range.Bytes.Length);
                current += range.Bytes.Length;
            }
            return bytes;
        }

        public void PushIntoProcessManager(ProcessManager manager, string username)
        {
            var ruleIndex = manager.FindRuleIndex(_offset);
            
            if (ruleIndex > -1)
            {
                var rule = manager.GetRule(ruleIndex);

                foreach (var range in this)
                {
                    if(rule.DataType == DataType.Data && rule.ChangeTrigger == ChangeTrigger.FlagOn)
                        manager.InjectFlagsOnFromExternal(ruleIndex, _offset, range.Bytes, range.Offset, username);
                    if (rule.DataType == DataType.Data && rule.ChangeTrigger == ChangeTrigger.FlagOff)
                        manager.InjectFlagsOffFromExternal(ruleIndex, _offset, range.Bytes, range.Offset, username);
                    else
                        manager.InjectDataFromExternal(ruleIndex, _offset, range.Bytes, range.Offset, username);
                }
            }
        }

        public static BytesDifferenceContainer ChangedBytesFromIndexSet(long offset, ISet<long> indexes, byte[] bytes)
        {
            var container = new BytesDifferenceContainer(offset);
            long blockStart = 0;
            long current = 0;
            long arraySize = bytes.LongLength;
            bool insideBlock = false;
            for(current = 0; current < arraySize; ++current)
            {
                if (indexes.Contains(current))
                {
                    if (!insideBlock)
                        blockStart = current;
                    insideBlock = true;
                }
                else if (insideBlock)
                {
                    var tempBytes = new byte[current - blockStart];
                    Array.Copy(bytes, blockStart, tempBytes, 0, current - blockStart);
                    container.Add(new ChangedBytesRange(tempBytes, offset + blockStart));
                    insideBlock = false;
                }
            }
            if (insideBlock)
            {
                var tempBytes = new byte[current - blockStart];
                Array.Copy(bytes, blockStart, tempBytes, 0, current - blockStart);
                container.Add(new ChangedBytesRange(tempBytes, offset + blockStart));
            }
            return container;
        }

    }

    public struct ChangedBytesRange
    {
        public byte[] Bytes;
        public long Offset;

        public ChangedBytesRange(byte[] bytes, long offset)
        {
            Bytes = bytes;
            Offset = offset;
        }
    }
}
