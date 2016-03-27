using MemoryExplorer.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Model
{
    public class DataModel : INotifyPropertyChanged
    {
        #region globals
        private bool _runningAsAdmin = false;
        private bool _liveCapture = false;
        private DataProviderBase _dataProvider = null;
        private string _memoryImageFilename = "";
        private DriverManager _driverManager = null;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region access
        public bool RunningAsAdmin { get { return _runningAsAdmin; } }
        public bool LiveCapture
        {
            get { return _liveCapture; }
            set { _liveCapture = value; }
        }
        public DataProviderBase DataProvider
        {
            get { return _dataProvider; }
            set { _dataProvider = value; }
        }
        public string MemoryImageFilename
        {
            get { return _memoryImageFilename; }
            set { SetProperty(ref _memoryImageFilename, value); }
        }
        #endregion
        public DataModel(bool IsAdmin)
        {
            _runningAsAdmin = IsAdmin;
        }
        public bool NewLiveInvestigation()
        {            
            if (_driverManager == null)
            {
                try
                {
                _driverManager = new DriverManager();
                _driverManager.LoadDriver();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error: Loading Driver. (DataModel:NewLiveInvestigation): " + ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            _liveCapture = true;
            MemoryImageFilename = "Live";
            _dataProvider = new LiveDataProvider(this);
            return true;
        }
        public bool NewImageInvestigation()
        {            
            string possibleFilename = GetImageFile();
            if (possibleFilename == null)
                return false;
            _liveCapture = false;
            BigCleanUp();
            MemoryImageFilename = possibleFilename;
            _dataProvider = new ImageDataProvider(this);
            return true;
        }
        private void BigCleanUp()
        {
            if (_driverManager != null)
            {
                _driverManager.UnloadDriver();
                _driverManager = null;
            }
        }
        private string GetImageFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image Files (vmem)|*.vmem|All FIles (*.*)|*.*";
            bool? clicked = dialog.ShowDialog();
            if(clicked == true)
            {
                
                return dialog.FileName;
            }
            return null;
        }
        protected virtual void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(member, val)) return;

            member = val;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
