namespace MemoryExplorer.Worker
{
    public enum JobStatus
    {
        Queued = 0x01,
        Running = 0x02,
        Complete = 0x03,
        Cancelled = 0x04,
        Failed = 0x05,
        Unknown = 0x06
    }
    public enum JobAction
    {
        Unknown = 0,
        LoadPlugin = 0x01,
        GetProfileIdentification = 0x02,
        SetDataProvider = 0x03,
        LoadProfile = 0x04,
        FindKernelDtb = 0x05,
        LoadKernelAddressSpace = 0x06,
        FindKernelImage = 0x07,
        FindUserSharedData = 0x08,
        EnumerateObjectTypes = 0x09,
        // ingester jobs
        LoadProfileId = 0x1000,
        SetCacheFolder = 0x1001
    }
}
