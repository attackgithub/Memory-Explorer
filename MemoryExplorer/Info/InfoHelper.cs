namespace MemoryExplorer.Info
{
    public enum InfoHelperType : uint
    {
        Unknown = 0,
        InfoDictionary = 1
    }
    public class InfoHelper
    {
        public string Name;
        public InfoHelperType Type;
        public ulong PhysicalAddress;
        public ulong VirtualAddress;
        public uint BufferSize;
        public InfoHelper()
        {
            PhysicalAddress = 0;
            VirtualAddress = 0;
            BufferSize = 0;
            Type = InfoHelperType.Unknown;
            Name = "";
        }
    }
}
