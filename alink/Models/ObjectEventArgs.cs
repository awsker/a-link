using System;

namespace alink.Models
{
    public class ObjectEventArgs:EventArgs
    {
        public object Object { get; private set; }

        public ObjectEventArgs(object o)
        {
            Object = o;
        }
    }
}
