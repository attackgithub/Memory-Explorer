using MemoryExplorer.Details;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer
{
    public class MainWindowViewModel : BindableBase
    {
        private BindableBase _currentDetailsViewModel;
        private Details1ViewModel _details1ViewModel = null;
        private Details2ViewModel _details2ViewModel = null;


        public MainWindowViewModel()
        {
            _dataModel = new DataModel(IsAdmin());
            _details1ViewModel = new Details1ViewModel();
            _details2ViewModel = new Details2ViewModel();
            _currentDetailsViewModel = _details2ViewModel;
        }
        private bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principle = new WindowsPrincipal(identity);
            return principle.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public BindableBase CurrentDetailsViewModel
        {
            get { return _currentDetailsViewModel; }
            set { SetProperty(ref _currentDetailsViewModel, value); }
        }
        //private void OnNav(string destination)
        //{
        //    switch (destination)
        //    {
        //        case "view1":
        //            CurrentRightViewModel = _rightSide1ViewModel;
        //            break;
        //        case "view2":
        //        default:
        //            CurrentRightViewModel = _rightSide2ViewModel;
        //            break;
        //    }
        //}
    }
}
