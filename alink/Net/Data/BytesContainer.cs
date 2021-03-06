﻿using System;
using alink.Models;
using alink.Utils;

namespace alink.Net.Data
{
    public class BytesContainer : IChangedDataContainer
    {
        private long _offset;
        private byte[] _bytes;

        public BytesContainer(byte[] bytes, long offset)
        {
            _bytes = bytes;
            _offset = offset;
        }

        public byte[] GetNetworkBytes()
        {
            var size = 1 + sizeof(long) * 2 + _bytes.Length;
            var bytes = new byte[size];
            bytes[0] = (byte) ChangeDataType.Bytes;
            int current = 1;
            ByteWriter.WriteLong(_offset, bytes, current);
            current += sizeof(long);
            ByteWriter.WriteLong(_bytes.Length, bytes, current);
            current += sizeof(long);
            Array.Copy(_bytes, 0, bytes, current, _bytes.Length);
            return bytes;
        }

        public void FillFromNetworkBytes(byte[] bytes)
        {
            int current = 1;
            _offset = BitConverter.ToInt64(bytes, current);
            current += sizeof(long);
            var numBytes = BitConverter.ToInt64(bytes, current);
            current += sizeof(long);
            _bytes = new byte[numBytes];
            Array.Copy(bytes, current, _bytes, 0, numBytes);
        }

        public void PushIntoProcessManager(ProcessManager manager, string username)
        {
            var ruleIndex = manager.FindRuleIndex(_offset);
            if (ruleIndex > -1)
            {
                var rule = manager.GetRule(ruleIndex);
                //Same offset since this container always contains all bytes of the memory rule
                if (rule.DataType == DataType.Data && rule.ChangeTrigger == ChangeTrigger.FlagOn)
                    manager.InjectFlagsOnFromExternal(ruleIndex, _offset, _bytes, _offset, username);
                if (rule.DataType == DataType.Data && rule.ChangeTrigger == ChangeTrigger.FlagOff)
                    manager.InjectFlagsOffFromExternal(ruleIndex, _offset, _bytes, _offset, username);
                else
                    manager.InjectDataFromExternal(ruleIndex, _offset, _bytes, _offset, username);
            }
        }
    }
}
