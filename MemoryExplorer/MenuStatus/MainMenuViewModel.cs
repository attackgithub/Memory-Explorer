using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.MenuStatus
{
    public class MainMenuViewModel : BindableBase
    {
        public MainMenuViewModel()
        {
            LiveRequest = new RelayCommand(OnLiveRequest);
            ImageRequest = new RelayCommand(OnImageRequest);
            ExitRequest = new RelayCommand(OnExitRequest);

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
