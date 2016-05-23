using MemoryExplorer.Address;

namespace MemoryExplorer.Details
{
    public class MemMapResult
    {
        private readonly string _virtualAddress;
        private readonly string _physicalAddress;
        private readonly string _blockSize;
        private readonly string _softwarePte;
        private readonly object _object = new object();
        public MemMapResult(AddressRecord record)
        {
            _virtualAddress = "0x" + record.VirtualAddress.ToString("X08").ToLower();
            _physicalAddress = "0x" + record.PhysicalAddress.ToString("X08").ToLower();
            _blockSize = "0x" + record.Size.ToString("X").ToLower() + " (" + record.Size.ToString() + ")";
            _softwarePte = record.IsSoftware ? "yes" : "no";
        }
        public string VirtualAddress { get { return _virtualAddress; } }
        public string PhysicalAddress { get { return _physicalAddress; } }
        public string BlockSize { get { return _blockSize; } }
        public string IsSoftware { get { return _softwarePte; } }
    }
}
