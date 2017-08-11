using System;

namespace alink.Models
{
    public class MemoryRule : ICloneable
    {
        private const int Version = 1;
        private string _description;
        private long _memoryOffset;
        private int _numBytes;
        private DataType _dataType;
        private ChangeTrigger _trigger;
        private TransferType _transferType;
        private Endianness _endian;
        private byte[] _bytes;
        private bool _printToLog;
        /*
        private long? _minValueToSend;
        private long? _maxValueToSend;
        private long? _minValueClamp;
        */

        public MemoryRule()
        {
            Description = string.Empty;
            MemoryOffset = "0";
            NumBytes = 0;
            DataType = DataType.Data;
            ChangeTrigger = ChangeTrigger.AnyChange;
            TransferType = TransferType.AllBytes;
            Endianness = Endianness.LittleEndian;
            Log = true;
        }

        public MemoryRule(string description, string memoryOffset, int numBytes, DataType dataType, ChangeTrigger trigger, TransferType transfer, Endianness endian, bool log)
            :this(description, Convert.ToInt64(memoryOffset, 16), numBytes, dataType, trigger, transfer, endian, log)
        {}

        public MemoryRule(string description, long memoryOffset, int numBytes, DataType dataType, ChangeTrigger trigger, TransferType transfer, Endianness endian, bool log)
        {
            Description = description;
            _memoryOffset = memoryOffset;
            _numBytes = numBytes;
            _dataType = dataType;
            _trigger = trigger;
            _transferType = transfer;
            _endian = endian;
            _bytes = null;
            _printToLog = log;
        }

        public MemoryRule(string serializedData)
        {
            var split = serializedData.Split('|');
            
            //Unused so far
            var version = split[0];
            
            _description = split[1];
            _memoryOffset = Convert.ToInt64(split[2], 16);
            _numBytes = Convert.ToInt32(split[3]);
            _dataType = (DataType)Enum.Parse(typeof (DataType), split[4]);
            _trigger = (ChangeTrigger)Enum.Parse(typeof(ChangeTrigger), split[5]);
            _transferType = (TransferType)Enum.Parse(typeof(TransferType), split[6]);
            _endian = (Endianness)Enum.Parse(typeof(Endianness), split[7]);
            if (split.Length > 8)
            {
                _printToLog = split[8] == "1";
            }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value.Replace("|", string.Empty); }
        }

        public string MemoryOffset
        {
            get { return _memoryOffset.ToString("X"); }
            set { _memoryOffset = Convert.ToInt64(value, 16); }
        }

        public long MemoryOffset64
        {
            get {  return _memoryOffset;}
            set { _memoryOffset = value; }
        }

        public int NumBytes
        {
            get { return _numBytes; }
            set { _numBytes = value; }
        }

        public DataType DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        public ChangeTrigger ChangeTrigger
        {
            get {  return _trigger;}
            set { _trigger = value; }
        }

        public TransferType TransferType
        {
            get { return _transferType; }
            set { _transferType = value; }
        }

        public Endianness Endianness
        {
            get { return _endian; }
            set { _endian = value; }
        }

        public bool Log
        {
            get { return _printToLog; }
            set { _printToLog = value; }
        }

        public MemoryRule Clone()
        {
            return new MemoryRule(_description, _memoryOffset, _numBytes, _dataType, _trigger, _transferType, _endian, _printToLog);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public string Serialize()
        {
            return string.Join("|", Version, _description, _memoryOffset.ToString("X"), _numBytes, _dataType, _trigger, _transferType, _endian, _printToLog ? 1:0);
        }

        public byte[] Bytes
        {
            get { return _bytes; }
            set { _bytes = value; }
        }
    }

    public enum DataType
    {
        Data,
        SignedInteger,
        UnsignedInteger,
        Decimal,
        Flags
    }

    public enum ChangeTrigger
    {
        AnyChange,
        Increase,
        Decrease,
        FlagOn,
        FlagOff
    }

    public enum TransferType
    {
        AllBytes,
        Difference
    }

    public enum Endianness
    {
        BigEndian,
        LittleEndian
    }
}
