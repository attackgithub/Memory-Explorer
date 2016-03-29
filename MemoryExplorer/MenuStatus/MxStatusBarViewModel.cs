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
        public string MemoryImageFilename { get { return _dataModel.MemoryImageFilename; } }
        public string ProfileName { get { return _dataModel.ProfileName; } }
        public string ActivityMessage { get { return _dataModel.ActivityMessage; } }
        public string Architecture { get { return _dataModel.Architecture; } }


    }
}
