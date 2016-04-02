using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MemoryExplorer.MenuStatus
{
    public class MenuItem
    {
        public string MenuItemText { get; set; }
        public ICommand MenuItemCommand { get; set; }
        public object MenuItemCommandParameter { get; set; }
    }
    public class MainMenuViewModel : BindableBase
    {
        public ObservableCollection<MenuItem> MenuItems = new ObservableCollection<MenuItem>();

        public MainMenuViewModel()
        {
            LiveRequest = new RelayCommand(OnLiveRequest);
            ImageRequest = new RelayCommand(OnImageRequest);
            ExitRequest = new RelayCommand(OnExitRequest);
            MruRequest = new RelayCommand<string>(OnMruRequest);            
        }

        public Visibility MruCollapsed
        {
            get
            {
                if (_dataModel.Mru.Count > 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }
        public ObservableCollection<MenuItem> MruMenuItems
        {
            get
            {
                MenuItems.Clear();
                int counter = 1;
                foreach (var item in _dataModel.Mru)
                {
                    MenuItem mi = new MenuItem();
                    mi.MenuItemText = (counter++).ToString() + " " + item;
                    mi.MenuItemCommand = MruRequest;
                    mi.MenuItemCommandParameter = item;
                    MenuItems.Add(mi);
                }

                return MenuItems;
            }
        }
        public string MruName
        {
            get { return "hello"; }
        }
        public bool IsLiveCapturePossible
        {
            get
            {
                if (_dataModel == null)
                    return false;
                return _dataModel.RunningAsAdmin;
            }
        }
        public RelayCommand<string> MruRequest { get; private set; }

        private void OnMruRequest(string name)
        {
            _dataModel.NewImageInvestigation(name);
        }
        public RelayCommand LiveRequest { get; private set; }
        private void OnLiveRequest()
        {

            if (_dataModel == null)
                MessageBox.Show("Error: Data Model is missing. (MainMenuViewModel:OnLiveRequest)", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else if(_dataModel.LiveCapture)
            {
                MessageBox.Show("You're Already Doing A Live Examination", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                _dataModel.NewLiveInvestigation();

        }
        public RelayCommand ImageRequest { get; private set; }

        private void OnImageRequest()
        {
            if (_dataModel == null)
                MessageBox.Show("Error: Data Model is missing. (MainMenuViewModel:OnImageRequest)", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else

                _dataModel.NewImageInvestigation();
        }
        public RelayCommand ExitRequest { get; private set; }

        private void OnExitRequest()
        {
            Application.Current.Shutdown();
        }
    }
}
