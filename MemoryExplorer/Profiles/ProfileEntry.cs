using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Profiles
{
    public class ProfileEntry
    {
        private ProfileEntry _parent;
        private string _name;
        private bool _expanded = false;
        private bool _selected = false;
        private uint _recordNumber;
        private uint _parentRecordNumber;
        private uint _offset;
        private uint _length;
        
        public ProfileEntry Parent
        {
            get { return _parent; }
            set
            {
                if (IsValidParent(value))
                    _parent = value;
            }
        }
        public bool IsValidParent(ProfileEntry candidate)
        {
            while (true)
            {
                if (candidate == null)
                    return true;
                if (candidate == this)
                    return false;
                candidate = candidate.Parent;
            }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public bool IsExpanded
        {
            get { return _expanded; }
            set { _expanded = value; }
        }
        public bool IsSelected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        public uint RecordNumber
        {
            get { return _recordNumber; }
            set { _recordNumber = value; }
        }
        public uint ParentRecordNumber
        {
            get { return _parentRecordNumber; }
            set { _parentRecordNumber = value; }
        }
        public uint Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        public uint Length
        {
            get { return _length; }
            set { _length = value; }
        }
    }
}
