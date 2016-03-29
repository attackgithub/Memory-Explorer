using MemoryExplorer.Artifacts;
using MemoryExplorer.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged
    {
        #region globals
        private bool _runningAsAdmin = false;
        private bool _liveCapture = false;
        private DataProviderBase _dataProvider = null;
        private string _memoryImageFilename = "";
        private DriverManager _driverManager = null;
        private List<ArtifactBase> _artifacts = new List<ArtifactBase>();
        private ArtifactBase _activeArtifact = null;
        private string _imageMd5 = "";
        private string _cacheLocation = "";
        private string _profileName = "";
        private string _activityMessage = "Idle";
        private int _activeJobCount = 0;
        private string _architecture = "";


        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region access
        public string Architecture
        {
            get { return _architecture; }
            set { SetProperty(ref _architecture, value); }
        }
        public string ActivityMessage
        {
            get { return _activityMessage; }
            set { SetProperty(ref _activityMessage, value); }
        }
        public string ProfileName
        {
            get { return _profileName; }
            set { SetProperty(ref _profileName, value); }
        }
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
        public List<ArtifactBase> Artifacts
        {
            get { return _artifacts; }
            set { SetProperty(ref _artifacts, value); }
        }
        #endregion
        public DataModel(bool IsAdmin)
        {
            _runningAsAdmin = IsAdmin;

            // some temporary test data
            RootArtifact ba = new RootArtifact();
            ba.Name = "Live Capture";
            ba.Parent = null;
            ba.IsExpanded = true;
            _artifacts.Add(ba);
            ProcessArtifact pa = new ProcessArtifact();
            pa.Name = "system.exe (12)";
            pa.Parent = ba;
            _artifacts.Add(pa);
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
            BigCleanUp();
            MemoryImageFilename = "Live";
            _dataProvider = new LiveDataProvider(this);            
            UpdateDetails(AddArtifact(ArtifactType.Root, "Live Capture", true));
            InitialSurvey();
            return true;
        }
        public bool NewImageInvestigation()
        {            
            string possibleFilename = GetImageFile();
            FileInfo fi = new FileInfo(possibleFilename);
            if (!fi.Exists)
                return false;
            IncrementActiveJobs();
            _imageMd5 = GetMD5HashFromFile(possibleFilename);
            _cacheLocation = fi.Directory.FullName + "\\[" + fi.Name + "]" + _imageMd5;
            DirectoryInfo di = new DirectoryInfo(_cacheLocation);
            if (!di.Exists)
                di.Create();

            _liveCapture = false;
            BigCleanUp();
            MemoryImageFilename = possibleFilename;
            _dataProvider = new ImageDataProvider(this);
            UpdateDetails(AddArtifact(ArtifactType.Root, fi.Name, true));
            DecrementActiveJobs();
            InitialSurvey();
            return true;
        }
        /// <summary>
        /// This function gets called everytime something gets selected in the tree view
        /// First do a check to see if the selected item is the currently selected item
        /// </summary>
        /// <param name="selectedArtifact"></param>
        public void UpdateDetails(ArtifactBase selectedArtifact)
        {
            if (selectedArtifact == null || selectedArtifact == _activeArtifact)
                return;
            _activeArtifact = selectedArtifact;

        }
        private void IncrementActiveJobs()
        {
            _activeJobCount++;
            ActivityMessage = "Busy";
        }
        private void DecrementActiveJobs()
        {
            if(_activeJobCount != 0)
                _activeJobCount--;
            if (_activeJobCount == 0)
                ActivityMessage = "Idle";


        }
        private string GetMD5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        private ArtifactBase AddArtifact(ArtifactType type, string name, bool selected = false, ArtifactBase parent=null)
        {
            ArtifactBase artifact;
            switch(type)
            {
                case ArtifactType.Root:
                    artifact = new RootArtifact();
                    break;
                case ArtifactType.Process:
                    artifact = new ProcessArtifact();
                    break;
                default:
                    return null;
            }
            artifact.Name = name;
            artifact.Parent = parent;
            artifact.IsExpanded = false;
            artifact.IsSelected = selected;
            _artifacts.Add(artifact);
            NotifyPropertyChange("TreeItems"); // this forces the set property / INotifyPropertyCHange
            return artifact;
        }
        #region cleanup
        private void BigCleanUp()
        {
            if (_driverManager != null)
            {
                _driverManager.UnloadDriver();
                _driverManager = null;
            }
            FlushArtifactsList();
            _activeArtifact = null;
            _imageMd5 = "";
            _cacheLocation = "";
        }
        private void FlushArtifactsList()
        {
            _artifacts.Clear();
        }
        #endregion
        private string GetImageFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image Files (vmem)|*.vmem|All FIles (*.*)|*.*";
            bool? clicked = dialog.ShowDialog();
            if(clicked == true)
                return dialog.FileName;
            return null;
        }
        #region INotifyPropertyCHange Helpers
        private void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(member, val)) return;

            member = val;
            NotifyPropertyChange(propertyName);
        }
        private void NotifyPropertyChange(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
