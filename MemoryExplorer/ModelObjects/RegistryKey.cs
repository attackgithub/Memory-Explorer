using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class RegistryKey : StructureBase
    {
        KeyControlBlock _kcb = null;
        string _name = "";

        public RegistryKey(Profile profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;            
            Initialise();
            ObjectHeader oh = new ObjectHeader(_profile);
            long headerSize = oh.Size;
            if (headerSize != -1)
                _header = new ObjectHeader(_profile, _dataProvider, _virtualAddress - (uint)headerSize);
        }
        public RegistryKey(Profile profile, DataProviderBase dataProvider, ObjectHeader header, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress)
        {
            _physicalAddress = physicalAddress;            
            Initialise();
        }

        private void Initialise()
        {
            Overlay("_CM_KEY_BODY");
            ulong keyControlBlockPtr = Members.KeyControlBlock & 0xffffffffffff;
            _kcb = new KeyControlBlock(_profile, _dataProvider, virtualAddress: keyControlBlockPtr);
            KeyControlBlock temp = _kcb;
            string name = "";
            while (temp != null)
            {
                name = temp.NameControlBlock.Name + "\\" + name;
                temp = temp.Parent;
            }
            _name = name.TrimEnd(new char[] { '\\' });

            //_is64 = (_profile.Architecture == "AMD64");
            //AddressBase addressSpace = _dataProvider.ActiveAddressSpace;
            //if (_virtualAddress != 0)
            //    _physicalAddress = addressSpace.vtop(_virtualAddress);
            //if (_physicalAddress == 0)
            //    throw new ArgumentException("Error - Address is ZERO for _CM_KEY_BODY");
            //_structureSize = (uint)_profile.GetStructureSize("_CM_KEY_BODY");
            //if (_structureSize == -1)
            //    throw new ArgumentException("Error - Profile didn't contain a definition for _CM_KEY_BODY");
            //if (_virtualAddress == 0)
            //    _buffer = _dataProvider.ReadPhysicalMemory(_physicalAddress, (uint)_structureSize);
            //else
            //    _buffer = _dataProvider.ReadMemoryBlock(_virtualAddress, (uint)_structureSize);
            //if (_buffer == null)
            //    return;
            //_structure = _profile.GetEntries("_CM_KEY_BODY");
            //Structure s = GetStructureMember("KeyControlBlock");
            //if (s.PointerType != "_CM_KEY_CONTROL_BLOCK")
            //    return;
            //if(_is64)
            //{
            //    ulong addr = BitConverter.ToUInt64(_buffer, (int)s.Offset) & 0xffffffffffff;
            //    _kcb = new KeyControlBlock(_profile, _dataProvider, addr);
            //    KeyControlBlock temp = _kcb;
            //    string name = "";
            //    while (temp != null)
            //    {
            //        name = temp.NameControlBlock.Name + "\\" + name;
            //        temp = temp.Parent;
            //    }
            //    _name = name.TrimEnd(new char[] { '\\' });
            //}
            //else
            //{

            //}
        }
        public ObjectHeader Header { get { return _header; } }
        public KeyControlBlock Kcb { get { return _kcb; } }
        public string Name { get { return _name; } }
    }
}
