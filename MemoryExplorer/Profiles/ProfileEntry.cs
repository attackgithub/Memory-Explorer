using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Profiles
{
    public class ProfileEntry
    {
        protected ProfileEntry _parent;
        protected string _name;
        protected bool _expanded = false;
        protected bool _selected = false;
        
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
    }
}
