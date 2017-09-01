using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using alink.Models;
using alink.Net.Data;
using Extemory;

namespace alink.Utils
{
    public class ProcessManager
    {
        private readonly Process _process;
        private readonly long _baseMemoryOffset;
        private RulesConfig _config;
        private RulesConfig _nextConfig;
        private int _pollIntervall;
        private bool _stopCalled;

        private BackgroundWorker _memoryWorker;

        public event EventHandler ProcessStarted;
        public event EventHandler ProcessStopped;

        public event EventHandler<MemoryChangedEventArgs> MemoryChanged;
        public event EventHandler<LogMessageEventArgs> LogOutput;

        public ProcessManager(Process p, MemoryOffset memoryOffset, RulesConfig config, int pollIntervall)
        {
            _process = p;
            if (memoryOffset.OffsetType == OffsetType.IntPointer)
            {
                _baseMemoryOffset = p.Read<int>(new IntPtr(memoryOffset.MemoryOffsetAddress));
                
            }
            else if (memoryOffset.OffsetType == OffsetType.LongPointer)
            {
                _baseMemoryOffset = p.Read<long>(new IntPtr(memoryOffset.MemoryOffsetAddress));
            }
            else
            {
                _baseMemoryOffset = memoryOffset.MemoryOffsetAddress;
            }
            _config = config;
            _pollIntervall = pollIntervall;
            _nextConfig = null;
        }

        public void SetRulesConfig(RulesConfig config)
        {
            _nextConfig = config;
        }

        public RulesConfig RulesConfig { get { return _config; } }
        public long BaseMemoryOffset { get { return _baseMemoryOffset; } }

        public bool Running
        {
            get { return _memoryWorker != null; }
        }


        public void Start()
        {
            if (_memoryWorker != null)
                return;
            _stopCalled = false;
            _memoryWorker = new BackgroundWorker();
            _memoryWorker.DoWork += run;
            _memoryWorker.RunWorkerCompleted += onComplete;
            _memoryWorker.RunWorkerAsync();
        }

        public void Stop()
        {
            if (_memoryWorker == null || _memoryWorker.CancellationPending)
                return;
            _stopCalled = true;
        }

        private void run(object o, DoWorkEventArgs args)
        {
            ProcessStarted?.Invoke(this, EventArgs.Empty);
            while (!_stopCalled)
            {
                lock (_config)
                {
                    foreach (var rule in _config.Rules)
                    {
                        IntPtr address = new IntPtr(_baseMemoryOffset + rule.MemoryOffset64);
                        var bytes = _process.ReadArray<byte>(address, rule.NumBytes);
                        if (rule.Bytes == null)
                        {
                            rule.Bytes = bytes;
                            continue;
                        }
                        else
                        {
                            IChangedDataContainer changedBytes;
                            if (isBytesChangedAccordingToRule(bytes, rule, out changedBytes))
                            {
                                rule.Bytes = bytes;
                                OnLog(rule, "You");
                                MemoryChanged?.Invoke(this, new MemoryChangedEventArgs(rule, changedBytes));
                            }
                            else
                                rule.Bytes = bytes;
                        }
                    }
                }
                Thread.Sleep(_pollIntervall);
                //Update config if request has been made
                if (_nextConfig != null)
                {
                    _config = _nextConfig;
                    _nextConfig = null;
                }
            }
        }

        private bool isBytesChangedAccordingToRule(byte[] bytes, MemoryRule rule, out IChangedDataContainer changedBytes)
        {
            if (rule.DataType == DataType.Decimal || rule.DataType == DataType.SignedInteger || rule.DataType == DataType.UnsignedInteger)
            {
                return isNumberChangedAccordingToRule(bytes, rule, out changedBytes);
            }
            if (rule.DataType == DataType.Data)
            {
                return isDataChangedAccordingToRule(bytes, rule, out changedBytes);
            }
            changedBytes = null;
            return false;
        }

        private bool isDataChangedAccordingToRule(byte[] bytes, MemoryRule rule, out IChangedDataContainer changedBytes)
        {
            ISet<long> changedBytesSet = new HashSet<long>();
            var count = bytes.LongLength;
            var sendAll = rule.TransferType == TransferType.AllBytes;
            for (long i = 0; i < count; ++i)
            {
                if (rule.Bytes[i] != bytes[i])
                {
                    if (rule.ChangeTrigger == ChangeTrigger.AnyChange ||
                        rule.ChangeTrigger == ChangeTrigger.FlagOn && (rule.Bytes[i] | bytes[i]) != rule.Bytes[i] ||
                        rule.ChangeTrigger == ChangeTrigger.FlagOff && (rule.Bytes[i] & bytes[i]) != rule.Bytes[i] ||
                        rule.ChangeTrigger == ChangeTrigger.Increase && rule.NumBytes == 1 && bytes[i] > rule.Bytes[i] ||
                        rule.ChangeTrigger == ChangeTrigger.Decrease && rule.NumBytes == 1 && bytes[i] < rule.Bytes[i])
                    {
                        changedBytesSet.Add(i);
                        if (sendAll)
                            break;
                    }
                }
            }
            if (changedBytesSet.Count > 0)
            {
                if (sendAll)
                    changedBytes = new BytesContainer(bytes, rule.MemoryOffset64);
                else
                    changedBytes = BytesDifferenceContainer.ChangedBytesFromIndexSet(rule.MemoryOffset64, changedBytesSet, bytes);
                return true;
            }
            else
            {
                changedBytes = null;
                return false;
            }
        }
        
        private bool isNumberChangedAccordingToRule(byte[] bytes, MemoryRule rule, out IChangedDataContainer changedBytes)
        {
            byte[] diff = null;
            bool numbersChanged = false;
            if (rule.DataType == DataType.Decimal)
            {
                if (bytes.Length == 4)
                {
                    var ov = ByteParser.ParseFloat(rule.Bytes, rule.Endianness);
                    var nv = ByteParser.ParseFloat(bytes, rule.Endianness);

                    diff = BitConverter.GetBytes(nv - ov);
                    numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                      rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                      rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);


                }
                else if (bytes.Length == 8)
                {
                    var ov = ByteParser.ParseDouble(rule.Bytes, rule.Endianness);
                    var nv = ByteParser.ParseDouble(bytes, rule.Endianness);

                    diff = BitConverter.GetBytes(nv - ov);
                    numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                      rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                      rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                }
                else
                {
                    throw new Exception("Can not parse decimal. Expected 4 or 8 bytes");
                }
            }
            else if (rule.DataType == DataType.SignedInteger || rule.DataType == DataType.UnsignedInteger)
            {
                bool signed = rule.DataType == DataType.SignedInteger;
                if (bytes.Length == 1)
                {
                    if (signed)
                    {
                        var ov = (sbyte) rule.Bytes[0];
                        var nv = (sbyte) bytes[0];

                        diff = BitConverter.GetBytes(nv - ov);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                    else
                    {
                        var ov = rule.Bytes[0];
                        var nv = bytes[0];

                        diff = BitConverter.GetBytes(nv - ov);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }

                }
                
                else if (bytes.Length == 2)
                {
                    if (signed)
                    {
                        var ov = ByteParser.ParseShort(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseShort(bytes, rule.Endianness);

                        diff = BitConverter.GetBytes(nv - ov);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                    else
                    {
                        var ov = ByteParser.ParseUnsignedShort(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseUnsignedShort(bytes, rule.Endianness);

                        diff = BitConverter.GetBytes(nv - ov);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }

                }
                else if (bytes.Length == 4)
                {
                    if (signed)
                    {
                        var ov = ByteParser.ParseInt(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseInt(bytes, rule.Endianness);

                        diff = BitConverter.GetBytes(nv - ov);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                    else
                    {
                        var ov = ByteParser.ParseUnsignedInt(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseUnsignedInt(bytes, rule.Endianness);
                        long diff2 = (long)nv - (long)ov;
                        diff = BitConverter.GetBytes((int)diff2);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                }
                else if (bytes.Length == 8)
                {
                    if (signed)
                    {
                        var ov = ByteParser.ParseLong(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseLong(bytes, rule.Endianness);

                        long diff2 = (long)nv - (long)ov;
                        diff = BitConverter.GetBytes((int)diff2);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                    else
                    {
                        var ov = ByteParser.ParseUnsignedLong(rule.Bytes, rule.Endianness);
                        var nv = ByteParser.ParseUnsignedLong(bytes, rule.Endianness);

                        long diff2 = (long)nv - (long)ov;
                        diff = BitConverter.GetBytes((int)diff2);
                        numbersChanged = (rule.ChangeTrigger == ChangeTrigger.AnyChange && !ov.Equals(nv) ||
                                          rule.ChangeTrigger == ChangeTrigger.Decrease && ov > nv ||
                                          rule.ChangeTrigger == ChangeTrigger.Increase && ov < nv);
                    }
                }
                else
                {
                    throw new Exception("Can not parse number. Expected 1, 2, 4 or 8 bytes");
                }
            }
            if (numbersChanged)
            {
                if (rule.TransferType == TransferType.AllBytes)
                {
                    changedBytes = new BytesContainer(bytes, rule.MemoryOffset64);
                    return true;
                }
                else
                {
                    changedBytes = new NumberDifferenceContainer(diff, rule.MemoryOffset64);
                    return true;
                }
            }
            changedBytes = null;
            return false;
        }

        public object GetDataFromRule(MemoryRule rule)
        {
            var bytes = GetBytes(rule);
            object data = null;
            if (rule.DataType == DataType.Data)
            {
                if (rule.NumBytes == 1)
                    data = bytes[0];
                else
                    data = bytes;
            }    
            else if (rule.DataType == DataType.Decimal)
            {
                if (rule.NumBytes == 4)
                    data = ByteParser.ParseFloat(bytes, rule.Endianness);
                if (rule.NumBytes == 8)
                    data = ByteParser.ParseDouble(bytes, rule.Endianness);
            }
            else if (rule.DataType == DataType.SignedInteger)
            {
                if (rule.NumBytes == 1)
                    data = (sbyte)bytes[0];
                if (rule.NumBytes == 2)
                    data = ByteParser.ParseShort(bytes, rule.Endianness);
                if (rule.NumBytes == 4)
                    data = ByteParser.ParseInt(bytes, rule.Endianness);
                if (rule.NumBytes == 8)
                    data = ByteParser.ParseLong(bytes, rule.Endianness);
            }
            else if (rule.DataType == DataType.UnsignedInteger)
            {
                if (rule.NumBytes == 1)
                    data = bytes[0];
                if (rule.NumBytes == 2)
                    data = ByteParser.ParseUnsignedShort(bytes, rule.Endianness);
                if (rule.NumBytes == 4)
                    data = ByteParser.ParseUnsignedInt(bytes, rule.Endianness);
                if (rule.NumBytes == 8)
                    data = ByteParser.ParseUnsignedLong(bytes, rule.Endianness);
            }
            if (data == null)
                throw new Exception("Could not parse data from rule");
            return data;
        }

        public byte[] GetBytes(MemoryRule rule)
        {
            var intPtr = new IntPtr(_baseMemoryOffset + rule.MemoryOffset64);
            var bytes = _process.ReadArray<byte>(intPtr, rule.NumBytes);
            return bytes;
        }

        private T getValue<T>(byte[] bytes, Endianness endian)
        {
            var genericType = typeof (T);
            if (genericType == typeof (float) && bytes.Length == 4)
                    return (T)Convert.ChangeType(ByteParser.ParseFloat(bytes, endian), typeof(float));
            if (genericType == typeof(double) && bytes.Length == 8)
                return (T)Convert.ChangeType(ByteParser.ParseDouble(bytes, endian), typeof(double));
            if (genericType == typeof (sbyte) && bytes.Length == 1)
                return (T) Convert.ChangeType(bytes[0], typeof (sbyte));
            if (genericType == typeof (byte) && bytes.Length == 1)
                return (T) Convert.ChangeType(bytes[0], typeof (byte));
            if (genericType == typeof(short) && bytes.Length == 2)
                return (T)Convert.ChangeType(ByteParser.ParseShort(bytes, endian), typeof(short));
            if (genericType == typeof(ushort) && bytes.Length == 2)
                return (T)Convert.ChangeType(ByteParser.ParseUnsignedShort(bytes, endian), typeof(ushort));
            if (genericType == typeof(int) && bytes.Length == 4)
                return (T)Convert.ChangeType(ByteParser.ParseInt(bytes, endian), typeof(int));
            if (genericType == typeof(uint) && bytes.Length == 4)
                return (T)Convert.ChangeType(ByteParser.ParseUnsignedInt(bytes, endian), typeof(uint));
            if (genericType == typeof(long) && bytes.Length == 8)
                return (T)Convert.ChangeType(ByteParser.ParseLong(bytes, endian), typeof(long));
            if (genericType == typeof(ulong) && bytes.Length == 8)
                return (T)Convert.ChangeType(ByteParser.ParseUnsignedLong(bytes, endian), typeof(ulong));

            throw new Exception("Invalid number of bytes for " + genericType.Name);
        }

        public void InjectDataFromExternal(int ruleIndex, long ruleMemoryOffset, byte[] bytes, long rangeMemoryOffset, string username)
        {
            lock (_config)
            {
                var rule = _config.Rules[ruleIndex];
                if (rule != null && rule.MemoryOffset64 == ruleMemoryOffset)
                {
                    _process.WriteArray(new IntPtr(_baseMemoryOffset + rangeMemoryOffset), bytes);
                    Array.Copy(bytes, 0, rule.Bytes, rangeMemoryOffset - ruleMemoryOffset, bytes.Length);
                    if(username != null)
                        OnLog(rule, username);
                }
            }
        }

        public void InjectFlagsOnFromExternal(int ruleIndex, long ruleMemoryOffset, byte[] bytes, long rangeMemoryOffset, string username)
        {
            lock (_config)
            {
                var rule = _config.Rules[ruleIndex];
                if (rule != null && rule.MemoryOffset64 == ruleMemoryOffset)
                {
                    var offset = new IntPtr(_baseMemoryOffset + rangeMemoryOffset);
                    var oldBytes = _process.ReadArray<byte>(offset, bytes.Length);
                    for (int i = 0; i < bytes.Length; ++i)
                    {
                        bytes[i] = (byte)(oldBytes[i] | bytes[i]);
                    }
                    _process.WriteArray(new IntPtr(_baseMemoryOffset + rangeMemoryOffset), bytes);
                    Array.Copy(bytes, 0, rule.Bytes, rangeMemoryOffset - ruleMemoryOffset, bytes.Length);
                    if (username != null)
                        OnLog(rule, username);
                }
            }
        }

        public void InjectFlagsOffFromExternal(int ruleIndex, long ruleMemoryOffset, byte[] bytes, long rangeMemoryOffset, string username)
        {
            lock (_config)
            {
                var rule = _config.Rules[ruleIndex];
                if (rule != null && rule.MemoryOffset64 == ruleMemoryOffset)
                {
                    var offset = new IntPtr(_baseMemoryOffset + rangeMemoryOffset);
                    var oldBytes = _process.ReadArray<byte>(offset, bytes.Length);
                    for (int i = 0; i < bytes.Length; ++i)
                    {
                        bytes[i] = (byte)(oldBytes[i] & bytes[i]);
                    }
                    _process.WriteArray(new IntPtr(_baseMemoryOffset + rangeMemoryOffset), bytes);
                    Array.Copy(bytes, 0, rule.Bytes, rangeMemoryOffset - ruleMemoryOffset, bytes.Length);
                    if (username != null)
                        OnLog(rule, username);
                }
            }
        }


        public void InjectDifferenceFromExternal(int ruleIndex, long ruleMemoryOffset, byte[] difference, string username)
        {
            lock (_config)
            {
                var rule = _config.Rules[ruleIndex];
                byte[] newBytes = null;
                if (rule != null && rule.MemoryOffset64 == ruleMemoryOffset)
                {
                    var address = new IntPtr(ruleMemoryOffset + _baseMemoryOffset);
                    var oldBytes = GetBytes(rule);
                    if (rule.DataType == DataType.Decimal)
                    {
                        if (rule.NumBytes == 4)
                        {
                            if(difference.Length != 4)
                                throw new Exception("Expected 4 bytes to parse float");

                            var diff = BitConverter.ToSingle(difference, 0);
                            var newVal = getValue<float>(oldBytes, rule.Endianness) + diff;
                            newBytes = BitConverter.GetBytes(newVal);
                        }
                        else if (rule.NumBytes == 8)
                        {
                            if (difference.Length != 8)
                                throw new Exception("Expected 8 bytes to parse double");

                            var diff = BitConverter.ToSingle(difference, 0);
                            var newVal = getValue<double>(oldBytes, rule.Endianness) + diff;
                            newBytes = BitConverter.GetBytes(newVal);
                        }
                    }
                    else if (rule.DataType == DataType.SignedInteger || rule.DataType == DataType.UnsignedInteger)
                    {
                        var signed = rule.DataType == DataType.SignedInteger;
                        if (difference.Length != 4)
                            throw new Exception("Expected 4 bytes to parse integer difference");
                        int diff = BitConverter.ToInt32(difference, 0);
                        if (rule.NumBytes == 1)
                        {
                            if (signed)
                            {
                                var newVal = _process.Read<sbyte>(address) + diff;
                                newBytes = new[] {(byte)clampSignedByte(newVal)};
                            }
                            else
                            {
                                var newVal = _process.Read<byte>(address) + diff;
                                newBytes = new[] { clampByte(newVal) };
                            }
                        }
                        else if (rule.NumBytes == 2)
                        {
                            if (signed)
                            {
                                var newVal = getValue<short>(oldBytes, rule.Endianness) + diff;
                                newBytes = BitConverter.GetBytes(clampShort(newVal));
                            }
                            else
                            {
                                var newVal = getValue<ushort>(oldBytes, rule.Endianness) + diff;
                                newBytes = BitConverter.GetBytes(clampUnsignedShort(newVal));
                            }
                        }
                        else if (rule.NumBytes == 4)
                        {
                            if (signed)
                            {
                                var newVal = (long)getValue<int>(oldBytes, rule.Endianness) + (long)diff;
                                newBytes = BitConverter.GetBytes(clampInt(newVal));
                            }
                            else
                            {
                                var newVal = getValue<uint>(oldBytes, rule.Endianness) + diff;
                                newBytes = BitConverter.GetBytes(clampUnsignedInt(newVal));
                            }
                        }
                        else if (rule.NumBytes == 8)
                        {
                            if (signed)
                            {
                                var newVal = getValue<long>(oldBytes, rule.Endianness) + diff;
                                newBytes = BitConverter.GetBytes((long)newVal);
                            }
                            else
                            {
                                throw new Exception("Invalid operation");
                                //var newVal = getValue<ulong>(oldBytes, rule.Endianness) + (long)diff;
                                //newBytes = BitConverter.GetBytes(newVal);
                            }
                        }
                    }
                    if (newBytes == null)
                    {
                        throw new Exception("Could not parse number");
                    }
                    IChangedDataContainer mock;
                    if(isNumberChangedAccordingToRule(newBytes, rule, out mock))
                    {
                        _process.WriteArray(address, newBytes);
                        rule.Bytes = newBytes;
                        if (username != null)
                            OnLog(rule, username);
                    }
                }
            }
        }

        private void OnLog(MemoryRule rule, string user)
        {
            if (!rule.Log)
                return;
            var message = $"{user} changed '{rule.Description}'";
            LogOutput?.Invoke(this, new LogMessageEventArgs(message));
        }

        private sbyte clampSignedByte(int val)
        {
            return (sbyte) Math.Max(sbyte.MinValue, Math.Min(val, sbyte.MaxValue));
        }

        private byte clampByte(int val)
        {
            return (byte)Math.Max(byte.MinValue, Math.Min(val, byte.MaxValue));
        }

        private short clampShort(int val)
        {
            return (short)Math.Max(short.MinValue, Math.Min(val, short.MaxValue));
        }

        private ushort clampUnsignedShort(int val)
        {
            return (ushort)Math.Max(ushort.MinValue, Math.Min(val, ushort.MaxValue));
        }

        private int clampInt(long val)
        {
            return (int)Math.Max(int.MinValue, Math.Min(val, int.MaxValue));
        }

        private uint clampUnsignedInt(long val)
        {
            return (uint)Math.Max(uint.MinValue, Math.Min(val, uint.MaxValue));
        }

        public int FindRuleIndex(long memoryOffset)
        {
            lock (_config)
            {
                for (int i = 0; i < _config.Rules.Count; ++i)
                {
                    if (_config.Rules[i].MemoryOffset64 == memoryOffset)
                        return i;
                }
            }
            return -1;
        }

        public MemoryRule GetRule(int index)
        {
            lock (_config)
            {
                return _config.Rules[index];
            }
        }

        private void onComplete(object o, RunWorkerCompletedEventArgs args)
        {
            _memoryWorker = null;
            ProcessStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
