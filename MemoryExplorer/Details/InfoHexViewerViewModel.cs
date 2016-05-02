using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Details
{
    public class InfoHexViewerViewModel : BindableBase
    {
        public DataModel DataModel { get { return _dataModel; } }
        public byte[] DataProvider { get { return _dataModel.CurrentInfoHexViewerContent; } }
        public ulong ActiveStartAddress { get { return _dataModel.CurrentInfoHexViewerContentAddress; } }
    }
}
