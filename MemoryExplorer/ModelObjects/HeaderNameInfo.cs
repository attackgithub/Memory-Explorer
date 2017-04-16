using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class HeaderNameInfo : StructureBase
    {
        private dynamic _hni;
        public HeaderNameInfo(DataModel model, ulong virtualAddress=0, ulong physicalAddress=0) : base(model, virtualAddress, physicalAddress)
        {
            _hni = _profile.GetStructure("_OBJECT_HEADER_NAME_INFO", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _hni; }
        }
        public string Name
        {
            get
            {
                try
                {
                    var name = _hni.Name;
                    UnicodeString us = new UnicodeString(_model, name.Buffer, name.Length, name.MaximumLength);
                    return us.Name;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract Name from current OBJECT_TYPE structure.");
                }
            }
        }
        public ulong ReferenceCount
        {
            get
            {
                try
                {
                    var referenceCount = _hni.ReferenceCount;
                    return (ulong)referenceCount;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract TotalNumberOfObjects from current OBJECT_TYPE structure.");
                }
            }
        }
        
    }
}
