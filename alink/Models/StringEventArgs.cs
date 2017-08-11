using System;

namespace alink.Models
{
    public class StringEventArgs : EventArgs
    {
        public string Message { get; }

        public StringEventArgs(string message)
        {
            Message = message;
        }
    }
}
