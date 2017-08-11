using alink.Utils;

namespace alink.Net.Data
{
    public interface IChangedDataContainer
    {
        byte[] GetNetworkBytes();
        void FillFromNetworkBytes(byte[] bytes);
        void PushIntoProcessManager(ProcessManager manager, string username);
    }

    public enum ChangeDataType
    {
        Bytes = 0,
        BytesDifference = 1,
        NumberDifference = 2
    }
}
