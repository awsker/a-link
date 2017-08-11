namespace alink.Net.Data
{
    public class IncomingMemoryData
    {
        public UserInfo User { get; }
        public IChangedDataContainer ChangedData { get; }

        public IncomingMemoryData(UserInfo user, IChangedDataContainer change)
        {
            User = user;
            ChangedData = change;
        }
    }
}
