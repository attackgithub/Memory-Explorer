using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MemoryExplorer.Details
{    
    public class RootDetailsViewModel : BindableBase
    {
        bool _infoSelected;
        bool _objectListSelected;
        bool _hexSelected;
        bool _debugSelected;
        public Visibility DebugVisible
        {
            get
            {
#if DEBUG
                return Visibility.Visible;
#else
                return Visibility.Collapsed;
#endif                
            }
        }
        
        public bool InfoSelected { get { return _infoSelected; } set { _infoSelected = value; } }
        public bool ObjectListSelected { get { return _objectListSelected; } set { _objectListSelected = value; } }
        public bool HexSelected { get { return _hexSelected; } set { _hexSelected = value; } }
        public bool DebugSelected { get { return _debugSelected; } set { _debugSelected = value; } }

    }

}
