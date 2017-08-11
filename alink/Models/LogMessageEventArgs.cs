using System;

namespace alink.Models
{
    public class LogMessageEventArgs:EventArgs
    {
        public string Message { get; }

        public LogMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
