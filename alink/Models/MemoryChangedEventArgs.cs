using System;
using alink.Net.Data;

namespace alink.Models
{
    public class MemoryChangedEventArgs : EventArgs
    {
        public MemoryRule MemoryRule { get; }
        public IChangedDataContainer ChangedData { get; }

        public MemoryChangedEventArgs(MemoryRule rule, IChangedDataContainer change)
        {
            MemoryRule = rule;
            ChangedData = change;
        }
    }
}
