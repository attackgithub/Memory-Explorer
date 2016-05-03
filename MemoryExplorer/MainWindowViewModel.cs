using MemoryExplorer.Details;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer
{
    public class MainWindowViewModel : BindableBase
    {
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
