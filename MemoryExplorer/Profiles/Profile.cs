using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MemoryExplorer.Profiles
{
    public class Profile
    {
        public object AccessLock = new object();
        private string _profileDllLocation;
        private DataModel _model;
        private Assembly _profileDll;
        private bool _active = false;
        private DataProviderBase _dataProvider;
        private dynamic _constantHelper = null;
        private dynamic _catalogueHelper = null;
        private string _architecture = null;
        private ulong _poolAlign = 0;
        private byte[] _dummyBuffer = null;
        private AddressBase _kernelAddressSpace;
        private List<ObjectTypeRecord> _objectTypeList = new List<ObjectTypeRecord>();

        public bool IsActive { get { return _active; } }
        public string Architecture { get { return _architecture; } }
        public ulong PoolAlign { get { return _poolAlign; } }
        public AddressBase KernelAddressSpace { get => _kernelAddressSpace; set => _kernelAddressSpace = value; }

        public Profile(string dllLocation, DataProviderBase provider, DataModel model)
        {
            _profileDllLocation = dllLocation;
            _model = model;
            _dataProvider = provider;
            _dummyBuffer = new byte[4096]; // this needs to be bigger than the bigest structure
            try
            {
                _profileDll = Assembly.LoadFile(dllLocation);
                InitialiseConstants();
                InitialiseCatalogue();
                GetArchitecture();
                LoadUsefulValues();
                _active = true;
            }
            catch
            {
                _active = false;
            }
        }
        private void InitialiseConstants()
        {
            foreach (Type type in _profileDll.GetExportedTypes())
            {
                if (type.FullName == @"LiveForensics.Symbols.MxSymbols")
                {
                    _constantHelper = Activator.CreateInstance(type);
                    return;
                }
            }
        }
        public uint GetConstant(string name)
        {
            if (_constantHelper == null)
                throw new ArgumentException("Profile couldn't load constants.");
            return _constantHelper.LookupConstant(name);
        }
        private void InitialiseCatalogue()
        {
            foreach (Type type in _profileDll.GetExportedTypes())
            {
                if (type.FullName == @"LiveForensics.Symbols.CatalogueInformation")
                {
                    _catalogueHelper = Activator.CreateInstance(type);
                    return;
                }
            }
        }
        #region Catalogue Access
        public string MachineType
        {
            get
            {
                if (_catalogueHelper == null)
                    return "Error - Catalogue Couldn't Be Loaded";
                return _catalogueHelper.MachineType;
            }
        }
        public string SymbolsFileName
        {
            get
            {
                if (_catalogueHelper == null)
                    return "Error - Catalogue Couldn't Be Loaded";
                return _catalogueHelper.SymbolsFileName;
            }
        }
        public uint Signature
        {
            get
            {
                if (_catalogueHelper == null)
                    return 0;
                return _catalogueHelper.Signature;
            }
        }
        public uint Age
        {
            get
            {
                if (_catalogueHelper == null)
                    return 0;
                return _catalogueHelper.Age;
            }
        }

        public string Contents
        {
            get
            {
                if (_catalogueHelper == null)
                    return "Error - Catalogue Couldn't Be Loaded";
                return _catalogueHelper.Contents;
            }
        }
        public Guid Guid
        {
            get
            {
                if (_catalogueHelper == null)
                    return Guid.Empty;
                return _catalogueHelper.Guid;
            }
        }
        public string Created
        {
            get
            {
                if (_catalogueHelper == null)
                    return "Error - Catalogue Couldn't Be Loaded";
                return _catalogueHelper.Created;
            }
        }

        public List<ObjectTypeRecord> ObjectTypeList { get => _objectTypeList; set => _objectTypeList = value; }




        #endregion
        public dynamic GetStructure(string name, byte[] buffer, int offset)
        {
            try
            {
                string target = "LiveForensics.Symbols." + name;
                if (buffer == null)
                    return null;
                if (offset > buffer.Length)
                    return null;
                foreach (Type type in _profileDll.GetExportedTypes())
                {
                    if (type.FullName == target)
                    {
                        dynamic c = Activator.CreateInstance(type, buffer, offset);
                        int size = c.MxStructureSize;
                        if ((buffer.Length - offset) < size)
                            return null;
                        return c;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        public dynamic GetStructure(string name, ulong imageOffset)
        {
            try
            {
                uint size = GetStructureSize(name);
                if (size > 0)
                {
                    byte[] buffer = _dataProvider.ReadPhysicalMemory(imageOffset, size);
                    return GetStructure(name, buffer, 0);
                }
                return null;
            }
            catch
            {
                return null;
            }
            
        }
        private void GetArchitecture()
        {
            try
            {
                uint size = GetStructureSize("_LIST_ENTRY");

                if (size == 8)
                {
                    _architecture = "I386";
                    _poolAlign = 8;
                }
                else
                {
                    _architecture = "AMD64"; // do the same as REKALL and default to AMD64 if you don't know any better
                    _poolAlign = 16;
                }
            }
            catch 
            {
                _architecture = "unknown";
            }
        }
        private void LoadUsefulValues()
        {
            _model.EprocessSize = GetStructureSize("_EPROCESS");
            _model.DriverObjectSize = GetStructureSize("_DRIVER_OBJECT");
            _model.HandleTableSize = GetStructureSize("_HANDLE_TABLE");
        }
        public uint GetStructureSize(string name)
        {
            try
            {
                dynamic st = GetStructure(name, _dummyBuffer, 0);
                return (uint)st.MxStructureSize;
            }
            catch 
            {
                return 0;
            }
        }
        public uint GetMemberOffset(string structure, string member)
        {
            try
            {                
                char[] v = { '.' };
                string[] parts = member.Split(v, StringSplitOptions.RemoveEmptyEntries);

                string activeStructure = structure;
                JArray targetNode;
                uint offsetCount = 0;
                foreach (string s in parts)
                {
                    var dict = GetDictionary(activeStructure);
                    targetNode = GetNode(activeStructure, s, dict);
                    offsetCount += (uint)((long)((JValue)targetNode[0]).Value);
                    activeStructure = ((JValue)targetNode[1][0]).Value.ToString();
                    if (activeStructure == "Pointer")
                        activeStructure = ((JValue)targetNode[1][1]["target"]).Value.ToString();

                }                    
                return offsetCount;
            }
            catch
            {
               return 0;
            }
        }
        private Dictionary<string, JToken> GetDictionary(string structure)
        {
            try
            {
                dynamic st = GetStructure(structure, _dummyBuffer, 0);
                string manifest = st.manifest;
                manifest = manifest.Replace("\r", "");
                manifest = manifest.Replace("\t", "");
                manifest = manifest.Replace("\n", "");
                manifest = manifest.Replace("(", "");
                manifest = manifest.Replace(")", "");

                object parsedObject = JsonConvert.DeserializeObject(manifest);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(parsedObject.ToString());
                return dict;
            }
            catch
            {
                return null;
            }
        }
        private JArray GetNode(string structure, string member, Dictionary<string, JToken> dict )
        {
            try
            {
                return (JArray)dict[structure][1].SelectToken(member);
            }
            catch (Exception)
            {
                throw new System.ArgumentException("Structure or Member Not Present");
            }                        
        }
    }
}

