using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer
{
    public class MainWindowViewModel
    {
        private DataModel _dataModel;

        public MainWindowViewModel()
        {
            _dataModel = new DataModel(IsAdmin());
        }
        private bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principle = new WindowsPrincipal(identity);
            return principle.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
