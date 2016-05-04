using MemoryExplorer.Info;
using MemoryExplorer.ModelObjects;

namespace MemoryExplorer.Details
{
    public class DriverResult
    {
        private readonly string _name;
        private readonly string _driverName;
        private readonly string _serviceName;
        private readonly string _size;
        private readonly string _handles;
        private readonly string _pointers;
        private readonly string _address;
        private readonly string _driverStart;
        private readonly string _driverStartPhysicalAddress;
        private readonly string _driverExtension;
        private readonly object _object = new object();

        public DriverResult(DriverObject driverObject)
        {
            _name = driverObject.Name;
            _driverName = driverObject.DriverName;
            _serviceName = driverObject.DriverExtension.ServiceKeyName;
            _size = "0x" + driverObject.DriverSize.ToString("X").ToLower() + " (" + driverObject.DriverSize.ToString() + ")";
            _handles = driverObject.HandleCount.ToString();
            _pointers = driverObject.PointerCount.ToString();
            _address = "0x" + driverObject.PhysicalAddress.ToString("X").ToLower();
            _driverStart = "0x" + driverObject.DriverStartPointer.ToString("X").ToLower();
            _driverStartPhysicalAddress = "0x" + driverObject.DriverStartPhysicalAddress.ToString("X").ToLower();
            _driverExtension = "0x" + driverObject.DriverExtensionVirtualAddress.ToString("X").ToLower();
            InfoHelper helper = new InfoHelper();
            helper.Type = InfoHelperType.DriverObject;
            helper.PhysicalAddress = driverObject.PhysicalAddress;
            helper.BufferSize = 1024; // need to get the actual driver object size
            helper.Title = "Driver Object";
            helper.TheObject = driverObject;
            _object = helper;
        }

        public string Name { get { return _name; } }

        public string DriverName { get { return _driverName; } }
        public string ServiceName { get { return _serviceName; } }
        public string Size { get { return _size; } }
        public string HandleCount { get { return _handles; } }
        public string PointerCount { get { return _pointers; } }
        public string PhysicalAddress { get { return _address; } }
        public string DriverStartAddress { get { return _driverStart; } }
        public string DriverStartPhysicalAddress { get { return _driverStartPhysicalAddress; } }
        public string DriverExtensionAddress { get { return _driverExtension; } }
        public object Helper { get { return _object; } }
    }
}
