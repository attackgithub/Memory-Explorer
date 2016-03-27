using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.MenuStatus
{
    public class MxStatusBarViewModel : BindableBase
    {
        public MxStatusBarViewModel()
        {

        }
        public string MemoryImageFilename
        {
            get
            {
                return _dataModel.MemoryImageFilename;
            }
        }
    }
}
