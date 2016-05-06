using System.ComponentModel;
using System.Windows;

namespace MemoryExplorer.Details
{
    public class MainContentViewModel : BindableBase
    {
        private BindableBase _currentDetailsViewModel;
        private RootDetailsViewModel _rootDetailsViewModel = null;
        private ProcessDetailsViewModel _processDetailsViewModel = null;

        public MainContentViewModel()
        {
            _rootDetailsViewModel = new RootDetailsViewModel();
            _processDetailsViewModel = new ProcessDetailsViewModel();
            _currentDetailsViewModel = _rootDetailsViewModel;
            // I shouldn't need this, but I just can't get the property change to get the view binding to update
            _dataModel.PropertyChanged += WtfPropertyChangedEventHandler;
        }
        private void WtfPropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentDetailsViewModel")
            {
                switch (_dataModel.CurrentDetailsViewModelHint)
                {
                    case "root":
                        CurrentDetailsViewModel = _rootDetailsViewModel;
                        break;
                    case "process":
                        CurrentDetailsViewModel = _processDetailsViewModel;
                        break;
                    default:
                        CurrentDetailsViewModel = _rootDetailsViewModel;
                        break;
                }
            }
        }
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
