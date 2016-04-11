using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Details
{
    public class HexViewerViewModel : BindableBase
    {
        public HexViewerViewModel()
        {

        }
        public DataModel DataModel { get { return _dataModel; } }
        public byte[] DataProvider { get { return _dataModel.CurrentHexViewerContent; } }
        public ulong ActiveStartAddress { get { return _dataModel.CurrentHexViewerContentAddress; } }
        public Visibility InterpreterWindowIsActive { get { return _dataModel.InterpreterWindowIsActive ? Visibility.Visible : Visibility.Collapsed; } }
    }
}
