using MemoryExplorer.Data;
using MemoryExplorer.Profiles;

namespace MemoryExplorer.ModelObjects
{
    public class MmVad : MmVadBase
    {
        private MmVadShort _shorty;
        private string _name = "";
        public MmVad(Profile_Deprecated profile, DataProviderBase dataProvider, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(profile, dataProvider, virtualAddress, physicalAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_MMVAD");
            byte[] vadShort = Members.Core;
            _shorty = new MmVadShort(_profile, _dataProvider, vadShort);
            VadProtection p = _shorty.Protection;
            VadType ty = _shorty.Type;
            Subsection ss = new Subsection(_profile, _dataProvider, virtualAddress: Members.Subsection & 0xffffffffffff);
            ulong controlArea = ss.Members.ControlArea & 0xffffffffffff;
            ControlArea ca = new ControlArea(_profile, _dataProvider, controlArea);
            ulong filePtr = ca.Members.FilePointer & 0xfffffffffff0; // THIS IS DODGY, NO IDEA WHY THIS WORKS   
            if(filePtr != 0)
            {
                FileObject fo = new FileObject(_profile, _dataProvider, filePtr);
                byte[] filename = fo.Members.FileName;
                UnicodeString us = new UnicodeString(_profile, _dataProvider, filename);
                _name = us.Name;
            }
        }
        public MmVadShort Core { get { return _shorty; } }
        public string Filename { get { return _name; } }
    }
}
