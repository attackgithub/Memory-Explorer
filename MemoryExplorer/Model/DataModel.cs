using MemoryExplorer.Address;
using MemoryExplorer.Artifacts;
using MemoryExplorer.Data;
using MemoryExplorer.HexView;
using MemoryExplorer.Info;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged, IDisposable
    {
        #region globals
        public bool ProcessPfnDatabase = false;
        public object AccessLock = new object();
        private bool _runningAsAdmin = false;
        private bool _liveCapture = false;
        private bool _interpreterWindowIsActive = false;
        private DataProviderBase _dataProvider = null;
        private string _memoryImageFilename = "";
        private DriverManager _driverManager = null;
        private List<ArtifactBase> _artifacts = new List<ArtifactBase>();
        private List<ProfileEntry> _profileEntries = new List<ProfileEntry>();
        private ArtifactBase _activeArtifact = null;
        private ArtifactBase _rootArtifact = null;
        private string _imageMd5 = "";
        private string _cacheLocation = "";
        private string _profileName = "";
        private string _activityMessage = "Idle";
        private int _activeJobCount = 0;
        private string _architecture = "";
        private Dictionary<string, InfoHelper> _infoDictionary = new Dictionary<string, InfoHelper>();
        private Dictionary<string, InfoHelper> _processInfoDictionary = new Dictionary<string, InfoHelper>();
        private ulong _kiUserSharedData = 0;
        private ulong _kernelDtb = 0;
        private Profile _profile = null;
        private List<string> _mru = new List<string>();
        private List<ProcessInfo> _processList = new List<ProcessInfo>();
        private AddressBase _kernelAddressSpace = null;
        private ulong _kernelBaseAddress = 0;
        private ulong _pfnDatabaseBaseAddress = 0;
        //private List<ObjectTypeRecord> _objectTypeList = new List<ObjectTypeRecord>();
        private byte[] _currentHexViewerContent = null;
        private ulong _currentHexViewerContentAddress = 0;
        private byte[] _currentInfoHexViewerContent = null;
        private ulong _currentInfoHexViewerContentAddress = 0;
        private string _currentDetailsViewModelHint = "";
        private PfnDatabase _pfnDatabase = null;
        private List<string> _debugTracer = new List<string>();
        private TabItem _rootDetailsSelectedTab = null;
        private List<DriverObject> _driverList = null;
        private string _tellMeAboutTitle = "Nothing to see here";
        private uint _eprocessSize = 0;
        private uint _driverObjectSize = 0;
        private uint _handleTableSize = 0;
        private List<HexViewHighlight> _infoHexHighlights = new List<HexViewHighlight>();
        private List<HexViewHighlight> _mainHexHighlights = new List<HexViewHighlight>();
        private ProcessInfo _selectedProcess = null;


        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        #region access
        public ProcessInfo ActiveProcess
        {
            get { return _selectedProcess; }
        }
        public ArtifactBase ActiveArtifact
        {
            get { return _activeArtifact; }
        }
        public TabItem RootDetailsSelectedTab
        {
            get { return _rootDetailsSelectedTab; }
            set { _rootDetailsSelectedTab = value; }
        }
        public string TellMeAboutTitle
        {
            get { return _tellMeAboutTitle; }
            set { SetProperty(ref _tellMeAboutTitle, value); }
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
        public ulong CurrentInfoHexViewerContentAddress
        {
            get { return _currentInfoHexViewerContentAddress; }
            set { _currentInfoHexViewerContentAddress = value; }
        }
        public byte[] CurrentInfoHexViewerContent
        {
            get { return _currentInfoHexViewerContent; }
            set { SetProperty(ref _currentInfoHexViewerContent, value); }
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
            get { if (_profile == null) return null;  return _profile.ObjectTypeList; }
            //set { _profile.ObjectTypeList = value; NotifyPropertyChange("ObjectTypes"); }
        }
        public string CacheLocation { get { return _cacheLocation; } set { _cacheLocation = value; } }
        public List<string> Mru
        {
            get { return _mru; }
            set { SetProperty(ref _mru, value); }
        }
        public Dictionary<string, InfoHelper> InfoDictionary
        {
            get { return _infoDictionary; }
            set { SetProperty(ref _infoDictionary, value); } }
        public Dictionary<string, InfoHelper> ProcessInfoDictionary
        {
            get { return _processInfoDictionary; }
            set { SetProperty(ref _processInfoDictionary, value); }
        }
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
        public List<ProfileEntry> ProfileEntries
        {
            get { return _profileEntries; }
            set { SetProperty(ref _profileEntries, value); }
        }
        public List<ProcessInfo> ProcessList { get { return _processList; } }
        public List<DriverObject> DriverList { get { return _driverList; } set { SetProperty(ref _driverList, value); } }

        public List<HexViewHighlight> InfoHexHighlights { get { return _infoHexHighlights; } }

        public Profile GetProfile { get { return _profile; } }

        #endregion
        public DataModel(bool IsAdmin)
        {
            _runningAsAdmin = IsAdmin;
            var mru = Properties.Settings.Default.MRU;
            if(mru != null)
            {
                AddDebugMessage("Reading MRU List Entries: " + mru.Count.ToString());
                foreach (string item in mru)
                {
                    if(item != "empty")
                        _mru.Add(item);
                }                   
            }            
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
            //ProcessProcesses();
            return true;
        }
        public bool NewImageInvestigation(string possibleFilename)
        {
            FileInfo fi = new FileInfo(possibleFilename);
            if (!fi.Exists)
                return false;
            IncrementActiveJobs("Initialising");
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
            //ProcessProcesses();
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
                _processInfoDictionary.Clear();
                NotifyPropertyChange("CurrentDetailsViewModel"); // this forces the set property / INotifyPropertyCHange  CurrentHexViewerContent   CurrentDetailsViewModel
                return;
            }
            ProcessArtifact pa = _activeArtifact as ProcessArtifact;
            if (pa != null)
            {
                CurrentDetailsViewModelHint = "process";
                _processInfoDictionary.Clear();
                UpdateProcessInfoDictionary(pa.LinkedProcess);
                _selectedProcess = pa.LinkedProcess;
                NotifyPropertyChange("CurrentDetailsViewModel"); 
                return;
            }

        }
        private void UpdateProcessInfoDictionary(ProcessInfo info)
        {
            if (info != null)
            {
                InfoHelper helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.ProcessInfoDictionary;
                helper.Title = "PID";
                helper.PhysicalAddress = info.PhysicalAddress;
                helper.Name = info.Pid.ToString();
                AddToProcessInfoDictionary("Pid", helper);

                helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.ProcessInfoDictionary;
                helper.Title = "Parent PID";
                helper.PhysicalAddress = info.PhysicalAddress;
                helper.Name = info.ParentPid.ToString();
                AddToProcessInfoDictionary("Parent Pid", helper);

                helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.ProcessInfoDictionary;
                helper.Title = "EPROCESS Physical Address";
                helper.PhysicalAddress = info.PhysicalAddress;
                helper.Name = "0x" + info.PhysicalAddress.ToString("X8").ToLower();
                AddToProcessInfoDictionary("EPROCESS Physical Address", helper);

                helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.ProcessInfoDictionary;
                helper.Title = "EPROCESS Virtual Address";
                helper.PhysicalAddress = info.PhysicalAddress;
                helper.Name = "0x" + info.VirtualAddress.ToString("X8").ToLower();
                AddToProcessInfoDictionary("EPROCESS VIrtual Address", helper);

                helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.ProcessInfoDictionary;
                helper.Title = "Directory Table Base";
                helper.PhysicalAddress = info.PhysicalAddress;
                helper.Name = "0x" + info.Dtb.ToString("X8").ToLower();
                AddToProcessInfoDictionary("Directory Table Base", helper);

                helper = new InfoHelper();
                helper.TheObject = info;
                helper.Type = InfoHelperType.HandleTable;
                helper.Title = "Handle Table Address";
                helper.VirtualAddress = info.HandleTableAddress;
                helper.BufferSize = (uint)_profile.GetStructureSize("_HANDLE_TABLE");
                helper.Name = "0x" + info.HandleTableAddress.ToString("X8").ToLower();
                AddToProcessInfoDictionary("Handle Table Address", helper);
            }
        }
        public void UpdateMru(string newEntry)
        {
            AddDebugMessage("MRU Update: " + newEntry);
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
        private void AddToInfoDictionary(string key, InfoHelper helper)
        {
            if (helper == null)
                return;
            lock(AccessLock)
            {
                InfoHelper testValue;
                int suffix = 1;
                bool trying = true;
                string alternativeKey = key;
                while (trying)
                {
                    trying = InfoDictionary.TryGetValue(alternativeKey, out testValue);
                    if (trying)
                        alternativeKey = key + (suffix++).ToString();
                }
                Dictionary<string, InfoHelper> _tempInfo = new Dictionary<string, InfoHelper>();
                foreach (var item in InfoDictionary)
                {
                    _tempInfo.Add(item.Key, item.Value);
                }
                _tempInfo.Add(alternativeKey, helper);
                InfoDictionary = _tempInfo;
            }            
        }
        private void AddToProcessInfoDictionary(string key, InfoHelper helper)
        {
            if (helper == null)
                return;
            lock (AccessLock)
            {
                InfoHelper testValue;
                int suffix = 1;
                bool trying = true;
                string alternativeKey = key;
                while (trying)
                {
                    trying = ProcessInfoDictionary.TryGetValue(alternativeKey, out testValue);
                    if (trying)
                        alternativeKey = key + (suffix++).ToString();
                }
                Dictionary<string, InfoHelper> _tempInfo = new Dictionary<string, InfoHelper>();
                foreach (var item in ProcessInfoDictionary)
                {
                    _tempInfo.Add(item.Key, item.Value);
                }
                _tempInfo.Add(alternativeKey, helper);
                ProcessInfoDictionary = _tempInfo;
            }
        }
        public string GetMD5HashFromFile(string filename)
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
            switch (type)
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
            lock (AccessLock)
            {                
                _artifacts.Add(artifact);                
            }
            NotifyPropertyChange("TreeItems"); // this forces the set property / INotifyPropertyCHange
            NotifyPropertyChange("Processes");
            return artifact;
        }
        private void OrderProcessArtifacts()
        {
            Dictionary<ulong, ProcessArtifact> paList = new Dictionary<ulong, ProcessArtifact>();
            List<ProcessArtifact> processList = new List<ProcessArtifact>();

            foreach (ArtifactBase artifact in _artifacts)
            {
                ProcessArtifact pa = artifact as ProcessArtifact;
                if (pa == null || pa.LinkedProcess == null)
                    continue;
                paList.Add(pa.LinkedProcess.Pid, pa);
                processList.Add(pa);
            }
            // check for parent
            ProcessArtifact parent;
            foreach (ProcessArtifact pa in processList)
            {
                if (paList.TryGetValue(pa.LinkedProcess.ParentPid,out parent))
                {
                    if(pa.LinkedProcess.ParentPid != 0)
                        pa.Parent = parent;
                }
            }
            // do I need to check for children now?
            NotifyPropertyChange("Processes");
        }
        private void AddObjectType(ObjectTypeRecord record)
        {
            lock(AccessLock)
            {
                _profile.ObjectTypeList.Add(record);
            }
            
        }
        public void AddDebugMessage(string message)
        {
#if DEBUG
            lock(AccessLock)
            {
                DateTime CurrentTime = DateTime.Now;
                string Timestamp = CurrentTime.ToString() + " - ";
                //string Timestamp = CurrentTime.ToString("yyyyMMddHHmmss - ", DateTimeFormatInfo.InvariantInfo);
                _debugTracer.Add(Timestamp + message);
                NotifyPropertyChange("DebugTracer"); // this forces the set property / INotifyPropertyCHange
            }            
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
            lock(AccessLock)
            {
                _processList.Add(process);
            }
            _rootArtifact.IsExpanded = true;
            ProcessArtifact pa = AddArtifact(ArtifactType.Process, process.ProcessName, false, _rootArtifact) as ProcessArtifact;
            pa.LinkedProcess = process;            
        }
        public ProcessInfo FindProcess(ulong pid)
        {
            foreach (ProcessInfo item in _processList)
            {
                if (item.Pid == pid)
                    return item;
            }
            return null;
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
            _shouldStop = true;
            FlushArtifactsList();
            _activeArtifact = null;
            _imageMd5 = "";
            if(_profile != null && _profile.ObjectTypeList != null)
                _profile.ObjectTypeList.Clear();
            _cacheLocation = "";
            _profile = null;
            _kernelDtb = 0;
            _infoDictionary.Clear();
            _processInfoDictionary.Clear();
            _kernelBaseAddress = 0;
            _architecture = "";
            _processList.Clear();
            _pfnDatabaseBaseAddress = 0;
            AddDebugMessage("BIG CLEANUP CALLED");
        }
        private void FlushArtifactsList()
        {
            lock(AccessLock)
            {
                _artifacts.Clear();
            }
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
            //Thread.Sleep(10);
            try
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
            catch { }
            
        }
        #endregion
    }
}
