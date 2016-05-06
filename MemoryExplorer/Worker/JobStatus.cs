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
}
