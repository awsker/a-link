using System;

namespace alink.Models
{
    public class MemoryOffset
    {
        private string _description;

        public string Description
        {
            get { return _description; }
            private set { _description = value.Replace("|", ""); }
        }

        public long MemoryOffsetAddress { get; }

        public OffsetType OffsetType { get; }

        public MemoryOffset(string description, string memoryOffset, OffsetType offsetType)
        {
            Description = description;
            MemoryOffsetAddress = Convert.ToInt64(memoryOffset, 16);
            OffsetType = offsetType;
        }

        public MemoryOffset(string description, long memoryOffset, OffsetType offsetType)
        {
            Description = description;
            MemoryOffsetAddress = memoryOffset;
            OffsetType = offsetType;
        }

        public MemoryOffset(string inputString)
        {
            var split = inputString.Split('|');
            Description = split[0];
            MemoryOffsetAddress = Convert.ToInt64(split[1], 16);
            OffsetType t;
            if (Enum.TryParse(split[2], out t))
                OffsetType = t;
            else
                OffsetType = OffsetType.IntPointer;
        }

        public string Serialize()
        {
            return Description + "|" + MemoryOffsetAddress.ToString("X") + "|" + OffsetType;
        }

        public override string ToString()
        {
            return Description;
        }
    }

    public enum OffsetType
    {
        IntPointer,
        LongPointer,
        AbsoluteOffset
    }
}
