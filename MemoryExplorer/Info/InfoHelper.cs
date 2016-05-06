namespace MemoryExplorer.Info
{
    public enum InfoHelperType : uint
    {
        Unknown = 0,
        InfoDictionary = 1,
        DriverObject = 2,
        ProcessObject = 3,
        ProcessInfoDictionary = 4,
        HandleTable = 5
    }
    public class InfoHelper
    {
        public string Name;
        public InfoHelperType Type;
        public ulong PhysicalAddress;
        public ulong VirtualAddress;
        public uint BufferSize;
        public string Title;
        public object TheObject;
        public InfoHelper()
        {
            PhysicalAddress = 0;
            VirtualAddress = 0;
            BufferSize = 0;
            Type = InfoHelperType.Unknown;
            Name = "";
            Title = "";
            TheObject = null;
        }
    }
}
