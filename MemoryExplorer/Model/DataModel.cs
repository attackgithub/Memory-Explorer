using MemoryExplorer.Address;
using MemoryExplorer.Artifacts;
using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged
    {
        #region globals
        private bool _runningAsAdmin = false;
        private bool _liveCapture = false;
        private bool _interpreterWindowIsActive = false;
        private DataProviderBase _dataProvider = null;
        private string _memoryImageFilename = "";
        private DriverManager _driverManager = null;
        private List<ArtifactBase> _artifacts = new List<ArtifactBase>();
        private ArtifactBase _activeArtifact = null;
        private ArtifactBase _rootArtifact = null;
        private string _imageMd5 = "";
        private string _cacheLocation = "";
        private string _profileName = "";
        private string _activityMessage = "Idle";
        private int _activeJobCount = 0;
        private string _architecture = "";
        private Dictionary<string, string> _infoDictionary = new Dictionary<string, string>();
        private ulong _kiUserSharedData = 0;
        private ulong _kernelDtb = 0;
        private Profile _profile = null;
        private List<string> _mru = new List<string>();
        private List<ProcessInfo> _processList = new List<ProcessInfo>();
        private AddressBase _kernelAddressSpace = null;
        private ulong _kernelBaseAddress = 0;
        private ulong _pfnDatabaseBaseAddress = 0;
        private List<ObjectTypeRecord> _objectTypeList = new List<ObjectTypeRecord>();
        private byte[] _currentHexViewerContent = null;
        private ulong _currentHexViewerContentAddress = 0;
        private string _currentDetailsViewModelHint = "";
        private PfnDatabase _pfnDatabase = null;
        private List<string> _debugTracer = new List<string>();
        private TabItem _rootDetailsSelectedTab = null;


        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region access
        public TabItem RootDetailsSelectedTab
        {
            get { return _rootDetailsSelectedTab; }
            set { _rootDetailsSelectedTab = value; }
        }

        public List<string> DebugTracer
        {
            get { return _debugTracer; }
            set { SetProperty(ref _debugTracer, value); }
        }
        public string CurrentDetailsViewModelHint
        {
            get { return _currentDetailsViewModelHint; }
            set { _currentDetailsViewModelHint = value; }
        }
        
        public ulong CurrentHexViewerContentAddress
        {
            get { return _currentHexViewerContentAddress; }
            set { _currentHexViewerContentAddress = value; }
        }
        public byte[] CurrentHexViewerContent
        {
            get { return _currentHexViewerContent; }
            set { SetProperty(ref _currentHexViewerContent, value); }
        }
        public List<ObjectTypeRecord> ObjectTypeList
        {
            get { return _objectTypeList; }
            set { SetProperty(ref _objectTypeList, value); }
        }
        public string CacheLocation { get { return _cacheLocation; } }
        public List<string> Mru
        {
            get { return _mru; }
            set { SetProperty(ref _mru, value); }
        }
        public Dictionary<string, string> InfoDictionary
        {
            get { return _infoDictionary; }
            set { SetProperty(ref _infoDictionary, value); } }
        public string Architecture
        {
            get { return _architecture; }
            set { SetProperty(ref _architecture, value); } }
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
        public bool InterpreterWindowIsActive
        {
            get { return _interpreterWindowIsActive; }
            set { SetProperty(ref _interpreterWindowIsActive, value); }
        }
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
        public string HelperLibrary
        {
            get
            {
                if (_driverManager == null)
                    return "";
                return _driverManager.LibraryFilename;
            }
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
            var mru = Properties.Settings.Default.MRU;
            if(mru != null)
            {
                foreach (string item in mru)
                {
                    if(item != "empty")
                        _mru.Add(item);
                }                   
            }

            // some temporary test data
            //RootArtifact ba = new RootArtifact();
            //ba.Name = "Live Capture";
            //ba.Parent = null;
            //ba.IsExpanded = true;
            //_artifacts.Add(ba);
            //ProcessArtifact pa = new ProcessArtifact();
            //pa.Name = "system.exe (12)";
            //pa.Parent = ba;
            //_artifacts.Add(pa);
        }
        public bool NewLiveInvestigation()
        {
            BigCleanUp();
            if (_driverManager == null)
            {
                try
                {
                    _driverManager = new DriverManager();
                    bool result = _driverManager.LoadDriver();
                    AddDebugMessage("Loaded Driver: " + result.ToString());
                    if(!result)
                    {
                        System.Windows.Forms.MessageBox.Show("Unable to perform live analysis.\nThere was a proble loading the driver", "DriverProblem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show("Error: Loading Driver. (DataModel:NewLiveInvestigation): " + ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            _liveCapture = true;
            MemoryImageFilename = "Live";
            _dataProvider = new LiveDataProvider(this);            
            UpdateDetails(_rootArtifact = AddArtifact(ArtifactType.Root, "Live Capture", true));
            InitialSurvey();
            return true;
        }
        public bool NewImageInvestigation(string possibleFilename)
        {
            FileInfo fi = new FileInfo(possibleFilename);
            if (!fi.Exists)
                return false;
            IncrementActiveJobs();
            BigCleanUp();
            _imageMd5 = GetMD5HashFromFile(possibleFilename);
            _cacheLocation = fi.Directory.FullName + "\\[" + fi.Name + "]" + _imageMd5;
            DirectoryInfo di = new DirectoryInfo(_cacheLocation);
            if (!di.Exists)
                di.Create();

            _liveCapture = false;
            MemoryImageFilename = possibleFilename;
            _dataProvider = new ImageDataProvider(this, _cacheLocation);
            UpdateDetails(_rootArtifact = AddArtifact(ArtifactType.Root, fi.Name, true));
            AddDebugMessage("New Image Loaded: " + possibleFilename);
            UpdateMru(MemoryImageFilename);
            DecrementActiveJobs();
            InitialSurvey();
            return true;
        }
        public bool NewImageInvestigation()
        {            
            string possibleFilename = GetImageFile();
            return NewImageInvestigation(possibleFilename);
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
            RootArtifact ra = _activeArtifact as RootArtifact;
            if(ra != null)
            {
                CurrentDetailsViewModelHint = "root";
                NotifyPropertyChange("CurrentDetailsViewModel"); // this forces the set property / INotifyPropertyCHange  CurrentHexViewerContent   CurrentDetailsViewModel
                return;
            }
            ProcessArtifact pa = _activeArtifact as ProcessArtifact;
            if (pa != null)
            {
                CurrentDetailsViewModelHint = "process";
                NotifyPropertyChange("CurrentDetailsViewModel"); 
                return;
            }

        }
        public void UpdateMru(string newEntry)
        {
            if (_mru.Contains(newEntry))
            {
                // it already exists, so move it to the top of the list
                _mru.Remove(newEntry);
                _mru.Insert(0, newEntry);
            }
            else
            {
                if (_mru.Count == 5)
                    _mru.RemoveAt(4);
                _mru.Insert(0, newEntry);
            }
            if (Properties.Settings.Default.MRU != null)
                Properties.Settings.Default.MRU.Clear();
            foreach (var item in _mru)
            {
                Properties.Settings.Default.MRU.Add(item);
            }
            Properties.Settings.Default.Save();
        }
        private void IncrementActiveJobs(string message = "")
        {
            _activeJobCount++;
            ActivityMessage = "Busy: " + message;
        }
        private void DecrementActiveJobs()
        {
            if(_activeJobCount != 0)
                _activeJobCount--;
            if (_activeJobCount == 0)
                ActivityMessage = "Idle";
        }
        private void AddToInfoDictionary(string key, string value)
        {
            if (value == null)
                return;
            string testValue;
            int suffix = 1;
            bool trying = true;
            string alternativeKey = key;
            while(trying)
            {
                trying = InfoDictionary.TryGetValue(alternativeKey, out testValue);
                if (trying)
                    alternativeKey = key + (suffix++).ToString();
            }
            Dictionary<string, string> _tempInfo = new Dictionary<string, string>();
            foreach (var item in InfoDictionary)
            {
                _tempInfo.Add(item.Key, item.Value);
            }
            _tempInfo.Add(alternativeKey, value);
            InfoDictionary = _tempInfo;
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
        
        private void AddObjectType(ObjectTypeRecord record)
        {
            _objectTypeList.Add(record);
            NotifyPropertyChange("ObjectTypes"); // this forces the set property / INotifyPropertyCHange
        }
        public void AddDebugMessage(string message)
        {
#if DEBUG
            DateTime CurrentTime = DateTime.Now;
            string Timestamp = CurrentTime.ToString() + " - ";
            //string Timestamp = CurrentTime.ToString("yyyyMMddHHmmss - ", DateTimeFormatInfo.InvariantInfo);
            _debugTracer.Add(Timestamp + message);
            NotifyPropertyChange("DebugTracer"); // this forces the set property / INotifyPropertyCHange
#else
            return;
#endif
        }
        #region PROCESSES
        private void FlushProcesses()
        {

        }
        private void AddProcess(ProcessInfo process)
        {
            _processList.Add(process);
            _rootArtifact.IsExpanded = true;
            ProcessArtifact pa = AddArtifact(ArtifactType.Process, process.ProcessName, false, _rootArtifact) as ProcessArtifact;
            pa.LinkedProcess = process;            

        }
        #endregion
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
            _profile = null;
            _kernelDtb = 0;
            _infoDictionary.Clear();
            _kernelBaseAddress = 0;
            _architecture = "";
            _objectTypeList.Clear();
            _pfnDatabaseBaseAddress = 0;
            AddDebugMessage("BIG CLEANUP CALLED");
        }
        private void FlushArtifactsList()
        {
            _artifacts.Clear();
        }
        #endregion
        private string GetImageFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
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
