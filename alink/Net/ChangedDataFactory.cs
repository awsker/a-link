using System;
using alink.Net.Data;

namespace alink.Net
{
    public static class ChangedDataFactory
    {
        public static IChangedDataContainer BuildChangedData(byte[] buffer)
        {
            if(buffer.Length == 0)
                throw new Exception("Empty buffer");

            if(buffer[0] > 2)
                throw new Exception("Invalid ChangeDataType");
            var type = (ChangeDataType) buffer[0];
            IChangedDataContainer container = null;
            if (type == ChangeDataType.Bytes)
            {
                container = new BytesContainer(new byte[0], 0);
                container.FillFromNetworkBytes(buffer);
                
            }
            if (type == ChangeDataType.BytesDifference)
            {
                container = new BytesDifferenceContainer(0);
                container.FillFromNetworkBytes(buffer);
            }
            if (type == ChangeDataType.NumberDifference)
            {
                container = new NumberDifferenceContainer(new byte[0], 0);
                container.FillFromNetworkBytes(buffer);
            }
            if(container == null)
                throw new Exception("Unable to build change data from supplied buffer");

            return container;
        }
    }
}
