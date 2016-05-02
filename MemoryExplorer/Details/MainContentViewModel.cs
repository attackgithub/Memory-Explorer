using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Details
{
    public class MainContentViewModel : BindableBase
    {
        private BindableBase _currentDetailsViewModel;
        private Details1ViewModel _details1ViewModel = null;
        private Details2ViewModel _details2ViewModel = null;
        private RootDetailsViewModel _rootDetailsViewModel = null;

        public MainContentViewModel()
        {
            //_dataModel = new DataModel(IsAdmin());
            _details1ViewModel = new Details1ViewModel();
            _details2ViewModel = new Details2ViewModel();
            _rootDetailsViewModel = new RootDetailsViewModel();
            _currentDetailsViewModel = _rootDetailsViewModel;
            // I shouldn't need this, but I just can't get the property change to get the view binding to update
            _dataModel.PropertyChanged += WtfPropertyChangedEventHandler;
        }
        private void WtfPropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            //Debug.WriteLine("Event: " + e.PropertyName);
            if (e.PropertyName == "CurrentDetailsViewModel")
            {
                switch (_dataModel.CurrentDetailsViewModelHint)
                {
                    case "root":
                        CurrentDetailsViewModel = _rootDetailsViewModel;
                        break;
                    case "process":
                        CurrentDetailsViewModel = _details1ViewModel;
                        break;
                    default:
                        CurrentDetailsViewModel = _rootDetailsViewModel;
                        break;
                }
            }
        }
        //private bool IsAdmin()
        //{
        //    WindowsIdentity identity = WindowsIdentity.GetCurrent();
        //    WindowsPrincipal principle = new WindowsPrincipal(identity);
        //    return principle.IsInRole(WindowsBuiltInRole.Administrator);
        //}
        public BindableBase CurrentDetailsViewModel
        {
            get { return _currentDetailsViewModel; }
            set { SetProperty(ref _currentDetailsViewModel, value); }
        }
        public Visibility InfoPaneActive
        {
            get { return Visibility.Visible; }
        }
    }
}
