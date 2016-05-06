using MemoryExplorer.Address;
using MemoryExplorer.Info;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using MemoryExplorer.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged, IDisposable
    {
        async private void InitialSurvey()
        {
            IncrementActiveJobs("Collecting Information");
            await GetInformation();
            DecrementActiveJobs();

            IncrementActiveJobs("Detecting Profile");
            await FindProfileGuid();
            DecrementActiveJobs();

            if (_profile == null)
                return;

            _eprocessSize = (uint)_profile.GetStructureSize("_EPROCESS");
            _driverObjectSize = (uint)_profile.GetStructureSize("_DRIVER_OBJECT");
            _handleTableSize = (uint)_profile.GetStructureSize("_HANDLE_TABLE");

            IncrementActiveJobs("Finding Kernel DTB");
            ulong eppa = await FindKernelDtb();
            DecrementActiveJobs();

            if (_kernelDtb == 0)
                return;

            IncrementActiveJobs("Loading Kernel Address Space");
            _kernelAddressSpace = await LoadKernelAddressSpace();
            _profile.KernelAddressSpace = _kernelAddressSpace;
            if (_kernelAddressSpace != null)
            {
                ProcessInfo p = new ProcessInfo();
                p.AddressSpace = _kernelAddressSpace;
                p.ProcessName = "System Idle Process";
                p.Pid = 0;
                p.PhysicalAddress = eppa;
                p.ParentPid = 0;
                p.Dtb = _kernelDtb;
                AddProcess(p);
            }
            DecrementActiveJobs();

            IncrementActiveJobs("Locating Kernel Image");
            await FindKernelImage();
            DecrementActiveJobs();
            _profile.KernelBaseAddress = _kernelBaseAddress;

            IncrementActiveJobs("Processing Shared Data");
            await FindUserSharedData();
            DecrementActiveJobs();

            IncrementActiveJobs("Detecting Object Types");
            await EnumerateObjectTypes();
            DecrementActiveJobs();

            IncrementActiveJobs("Processing Pfn Database");
            await LocatePfnDatabase();
            DecrementActiveJobs();

            IncrementActiveJobs("Process List One");
            await PsList_Method1();
            DecrementActiveJobs();
            OrderProcessArtifacts();

            IncrementActiveJobs("Process List Two");
            await PsList_Method2();
            DecrementActiveJobs();
            OrderProcessArtifacts();

            IncrementActiveJobs("Process List Three");
            await PsList_Method3();
            DecrementActiveJobs();
            OrderProcessArtifacts();

            IncrementActiveJobs("Process List Four");
            await PsList_Method4();
            DecrementActiveJobs();
            OrderProcessArtifacts();

            IncrementActiveJobs("Driver Scan");
            await ScanForDrivers();
            DecrementActiveJobs();

            ProcessProcesses();
        }

        

        async private Task GetInformation()
        {
            await Task.Run(() => 
            {
                Dictionary<string, object> info = _dataProvider.GetInformation();
                string friendlyKey;
                foreach (var item in info)
                {
                    if (item.Key == "dtb")
                    {
                        friendlyKey = "Directory Table Base";
                        _kernelDtb = (ulong)item.Value;
                    }
                    else if (item.Key == "buildNumber")
                        friendlyKey = "Build Number";
                    else if (item.Key == "kernelBase")
                    {
                        _kernelBaseAddress = (ulong)item.Value;
                        friendlyKey = "Kernel Base Address";
                        if (_kernelBaseAddress == 0)
                            continue;
                    }
                    else if (item.Key == "kdbg")
                        friendlyKey = "KDBG";
                    else if (item.Key == "pfnDatabase")
                    {
                        _pfnDatabaseBaseAddress = (ulong)item.Value;
                        friendlyKey = "PFN Database Address";
                        if (_pfnDatabaseBaseAddress == 0)
                            continue;
                    }
                    else if (item.Key == "psLoadedModuleList")
                        friendlyKey = "PsLoadedModuleList Address";
                    else if (item.Key == "ntBuildNumberAddress")
                        friendlyKey = "NtBuildNumberAddress";
                    else if (item.Key == "maximumPhysicalAddress")
                        friendlyKey = "Maximum Physical Address";
                    else
                        friendlyKey = item.Key;
                    ulong friendlyValue = (ulong)item.Value;
                    InfoHelper helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Title = friendlyKey;
                    helper.Name = "0x" + friendlyValue.ToString("X08") + " (" + friendlyValue.ToString() + ")";
                    AddToInfoDictionary(friendlyKey, helper);
                }
            });
        }
        async private Task FindProfileGuid()
        {
            await Task.Run(() => 
            {
                if (_dataProvider.IsLive)
                {
                    try
                    {
                        // get the system folder
                        string systemDirectory = Environment.SystemDirectory;
                        string kernelLocation = systemDirectory + "\\ntoskrnl.exe";
                        int matches = 0;
                        using (FileStream fs = new FileStream(kernelLocation, FileMode.Open, FileAccess.Read))
                        {
                            while (true)
                            {
                                byte b = (byte)fs.ReadByte();
                                if (matches == 0 && b == 82) // R
                                    matches = 1;
                                else if (matches == 1 && b == 83) // S
                                    matches = 2;
                                else if (matches == 2 && b == 68) // D
                                    matches = 3;
                                else if (matches == 3 && b == 83) // S
                                {
                                    byte[] buffer = new byte[16];
                                    int result = fs.Read(buffer, 0, 16);
                                    Guid g = new Guid(buffer);
                                    buffer = new byte[4];
                                    result = fs.Read(buffer, 0, 4);
                                    uint age = BitConverter.ToUInt32(buffer, 0);
                                    buffer = new byte[12];
                                    result = fs.Read(buffer, 0, 12);
                                    string name = Encoding.Default.GetString(buffer);
                                    if (name == "ntkrnlpa.pdb" || name == "ntkrnlmp.pdb" || name == "ntkrpamp.pdb" || name == "ntoskrnl.pdb")
                                    {
                                        string GuidAge = (g.ToString("N") + age.ToString()).ToUpper();
                                        ProfileName = GuidAge + ".gz";
                                        InfoHelper helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = ProfileName;
                                        helper.Title = "Profile Name";
                                        AddToInfoDictionary("ProfileName", helper);
                                        _profile = new Profile(ProfileName, @"E:\Forensics\MxProfileCache"); // TO DO - make this a user option when you get around to writing the settings dialog
                                        Architecture = _profile.Architecture;
                                        Architecture = _profile.Architecture;
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = Architecture;
                                        helper.Title = "Architecture";
                                        AddToInfoDictionary("Architecture", helper);
                                        if (_profile.Architecture == "I386")
                                        {
                                            _kiUserSharedData = 0xFFDF0000;
                                            _profile.PoolAlign = 8;
                                        }
                                        else
                                        {
                                            _kiUserSharedData = 0xFFFFF78000000000;
                                            _profile.PoolAlign = 16;
                                        }
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = "0x" + _kiUserSharedData.ToString("X");
                                        helper.Title = "KiUserSharedData";
                                        AddToInfoDictionary("KiUserSharedData", helper);
                                        return;
                                    }
                                    matches = 0;                                    
                                }
                                else
                                    matches = 0;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
                else
                {
                    StringSearch mySearch = new StringSearch(_dataProvider);
                    mySearch.AddNeedle("RSDS");
                    //Dictionary<string, List<ulong>> results = mySearch.Scan();
                    foreach (var answer in mySearch.Scan())
                    {
                        try
                        {
                            List<ulong> hitList = answer["RSDS"];
                            foreach (ulong hit in hitList)
                            {
                                try
                                {
                                    RSDS rsds = new RSDS(_dataProvider, hit);
                                    if (rsds.Signature == "RSDS" && (rsds.Filename == "ntkrnlpa.pdb" || rsds.Filename == "ntkrnlmp.pdb" || rsds.Filename == "ntkrpamp.pdb" || rsds.Filename == "ntoskrnl.pdb"))
                                    {
                                        InfoHelper helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = "0x" + hit.ToString("X08") + " (p)";
                                        helper.PhysicalAddress = hit;
                                        helper.BufferSize = 36;
                                        helper.Title = "Debug Symbols (RSDS)";
                                        AddToInfoDictionary("Debug Symbols (RSDS): ", helper);
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = rsds.Filename;
                                        helper.Title = "Debug Symbols Filename";
                                        AddToInfoDictionary("Debug Symbols Filename: ", helper);
                                        ProfileName = rsds.GuidAge + ".gz";
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = ProfileName;
                                        helper.Title = "Profile Name";
                                        AddToInfoDictionary("ProfileName", helper);
                                        _profile = new Profile(ProfileName, @"E:\Forensics\MxProfileCache"); // TO DO - make this a user option when you get around to writing the settings dialog
                                        Architecture = _profile.Architecture;
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = Architecture;
                                        helper.Title = "Architecture";
                                        AddToInfoDictionary("Architecture", helper);
                                        if (_profile.Architecture == "I386")
                                        {
                                            _kiUserSharedData = 0xFFDF0000;
                                            _profile.PoolAlign = 8;
                                        }
                                        else
                                        {
                                            _kiUserSharedData = 0xFFFFF78000000000;
                                            _profile.PoolAlign = 16;
                                        }
                                        helper = new InfoHelper();
                                        helper.Type = InfoHelperType.InfoDictionary;
                                        helper.Name = "0x" + _kiUserSharedData.ToString("X");
                                        helper.Title = "KiUserSharedData";
                                        helper.VirtualAddress = _kiUserSharedData;
                                        helper.BufferSize = 4096;
                                        AddToInfoDictionary("KiUserSharedData", helper);
                                        return;
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                    }
                }
            });
        }
        async private Task<ulong> FindKernelDtb()
        {
            ulong physicalAddress = 0;
            bool escape = false;
            await Task.Run(() =>
            {
                // check if we already have it (from the live image)
                if (_kernelDtb == 0)
                {
                    ulong filenameOffset = _profile.GetOffset("_EPROCESS", "ImageFileName");
                    StringSearch mySearch = new StringSearch(_dataProvider);
                    mySearch.AddNeedle("Idle\x00\x00\x00\x00\x00\x00\x00");

                    foreach (var answer in mySearch.Scan())
                    {
                        if (escape)
                            break;
                        try
                        {
                            List<ulong> hitList = answer.First().Value;
                            foreach (ulong hit in hitList)
                            {
                                try
                                {
                                    EProcess ep = new EProcess(_profile, _dataProvider, 0, hit - filenameOffset);
                                    _kernelDtb = ep.DTB;
                                    if (_kernelDtb > _dataProvider.ImageLength || _kernelDtb == 0)
                                    {
                                        _kernelDtb = 0;
                                        continue;
                                    }
                                    if (ep.Pid != 0 || ep.Ppid != 0)
                                    {
                                        _kernelDtb = 0;
                                        continue;
                                    }
                                    InfoHelper helper = new InfoHelper();
                                    helper.Type = InfoHelperType.InfoDictionary;
                                    helper.Name = "0x" + _kernelDtb.ToString("X08") + " (" + _kernelDtb.ToString() + ")";
                                    helper.Title = "Directory Table Base";
                                    AddToInfoDictionary("Directory Table Base", helper);
                                    //helper = new InfoHelper();
                                    //helper.Type = InfoHelperType.InfoDictionary;
                                    //helper.Name = ep.Pid.ToString();
                                    //helper.Title = "PID";
                                    //AddToInfoDictionary("PID", helper);
                                    //helper = new InfoHelper();
                                    //helper.Type = InfoHelperType.InfoDictionary;
                                    //helper.Name = ep.Ppid.ToString();
                                    //helper.Title = "Parent PID";
                                    //AddToInfoDictionary("Parent PID", helper);
                                    physicalAddress = (ulong)ep.PhysicalAddress;
                                    escape = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    continue;
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }                                                 
            });
            return physicalAddress;
        }
        async private Task<AddressBase> LoadKernelAddressSpace()
        {
            AddressBase addressSpace = null;
            await Task.Run(() =>
            {
                if (_profile.Architecture == "I386")
                    addressSpace = new AddressSpacex86Pae(_dataProvider, "idle", _kernelDtb, true);
                else
                    addressSpace = new AddressSpacex64(_dataProvider, "idle", _kernelDtb, true);
            });
            return addressSpace;
        }
        async private Task FindKernelImage()
        {
            await Task.Run(() =>
            {                                
                try
                {
                    uint buildOffset = (uint)_profile.GetConstant("NtBuildLab");
                    // first check that we haven't already got it - the live info grab will have got it!             
                    if (_kernelBaseAddress != 0)
                    {
                        ulong pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset, true);
                        if (pAddr == 0)
                            return;
                        byte[] buffer2 = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                        string build = ReadString(buffer2, (uint)(pAddr & 0xfff));
                        InfoHelper helper = new InfoHelper();
                        helper.Type = InfoHelperType.InfoDictionary;
                        helper.Name = build;
                        helper.Title = "Build String";
                        AddToInfoDictionary("Build String", helper);
                        try
                        {
                            uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                            pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset2, true);
                            if (pAddr == 0)
                                return;
                            buffer2 = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                            build = ReadString(buffer2, (uint)(pAddr & 0xfff));
                            helper = new InfoHelper();
                            helper.Type = InfoHelperType.InfoDictionary;
                            helper.Name = build;
                            helper.Title = "Build String Ex";
                            AddToInfoDictionary("Build String Ex", helper);
                        }
                        catch { }                        
                        return;
                    }

                    StringSearch mySearch = new StringSearch(_dataProvider);
                    mySearch.AddNeedle("INITKDBG");
                    mySearch.AddNeedle("MISYSPTE");
                    mySearch.AddNeedle("PAGEKD");
                    byte[] buffer = null;
                    foreach (var answer in mySearch.Scan())
                    {
                        foreach (var kvp in answer)
                        {
                            List<ulong> hitList = kvp.Value;
                            foreach (ulong hit in hitList)
                            {
                                // the physical address must exist in the kernel address space space
                                ulong vAddr = _kernelAddressSpace.ptov(hit);
                                if (vAddr == 0)
                                    continue;
                                //// let's grab the PE header while we're here
                                ulong page = vAddr & 0xfffffffff000;
                                // remember PE images are page aligned
                                for (int i = 0; i < 10; i++) // need to think about 10 being enough
                                {
                                    ulong tryAddress = _kernelAddressSpace.vtop(page, false);
                                    buffer = _dataProvider.ReadMemory(tryAddress, 1);
                                    string sig = Encoding.Default.GetString(buffer, 0, 2);
                                    if(sig == "MZ")
                                    {
                                        PE peHeader = new PE(_dataProvider, _kernelAddressSpace, page);
                                        RSDS debugSection = peHeader.DebugSection;
                                        if (IsValidKernel(debugSection.Filename))
                                        {
                                            _kernelBaseAddress = page;
                                            ulong pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset, false);
                                            if (pAddr == 0)
                                                continue;
                                            buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                            //CurrentHexViewerContentAddress = pAddr & 0xfffffffff000;
                                            //CurrentHexViewerContent = buffer;
                                            string build = ReadString(buffer, (uint)(pAddr & 0xfff));
                                            InfoHelper helper = new InfoHelper();
                                            helper.Type = InfoHelperType.InfoDictionary;
                                            helper.Name = "0x" + _kernelBaseAddress.ToString("X08");
                                            helper.Title = "Kernel Base Address";
                                            helper.VirtualAddress = _kernelBaseAddress;
                                            helper.BufferSize = 4096;
                                            AddToInfoDictionary("Kernel Base Address", helper);
                                            helper = new InfoHelper();
                                            helper.Type = InfoHelperType.InfoDictionary;
                                            helper.Name = build;
                                            helper.Title = "Build String";
                                            AddToInfoDictionary("Build String", helper);
                                            try
                                            {
                                                uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                                                pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset2, true);
                                                if (pAddr == 0)
                                                    continue;
                                                buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                                build = ReadString(buffer, (uint)(pAddr & 0xfff));
                                                helper = new InfoHelper();
                                                helper.Type = InfoHelperType.InfoDictionary;
                                                helper.Name = build;
                                                helper.Title = "Build String Ex";
                                                AddToInfoDictionary("Build String Ex", helper);
                                            }
                                            catch { }
                                            return;
                                        }
                                    }
                                    // move backwards one page at a time
                                    page -= 0x1000;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    return;
                }
            });
        }        
        async private Task FindUserSharedData()
        {
            await Task.Run(() =>
            {
                try
                {
                    ulong pAddr = _kernelAddressSpace.vtop(_kiUserSharedData, _liveCapture);
                    if (pAddr == 0)
                        return;
                    KUserSharedData kusd = new KUserSharedData(_dataProvider , _profile, pAddr);
                    string version = kusd.Version;
                    version = VersionHelper(version);

                    InfoHelper helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Name = version;
                    helper.Title = "Version";
                    AddToInfoDictionary("Version", helper);
                    var pageCount = kusd.Get("NumberOfPhysicalPages");
                    helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Name = pageCount.ToString();
                    helper.Title = "Physical Page Count";
                    AddToInfoDictionary("Physical Page Count", helper);
                    string rootDir = kusd.GetString("NtSystemRoot");
                    helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Name = rootDir;
                    helper.Title = "System Root";
                    AddToInfoDictionary("System Root", helper);
                    var procCount = kusd.Get("ActiveProcessorCount");
                    helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Name = procCount.ToString();
                    helper.Title = "Active Processor Count";
                    AddToInfoDictionary("Active Processor Count", helper);


                    return;
                }
                catch (Exception)
                {

                    return;
                }
            });
        }
        async private Task EnumerateObjectTypes()
        {
            await Task.Run(() =>
            {
                try
                {
                    ObjectTypes objectTypes = new ObjectTypes(_dataProvider, _profile);
                    if (objectTypes.Records != null && objectTypes.Records.Count > 0)
                    {
                        foreach (var record in objectTypes.Records)
                        {
                            AddObjectType(record);
                        }
                    }
                    return;
                }
                catch (Exception)
                {
                    return;
                }
            });
        }
        async private Task LocatePfnDatabase()
        {
            await Task.Run(() =>
            {
                try
                {
                    uint pfnAddressOffset = (uint)_profile.GetConstant("MmPfnDatabase");
                    ulong pfnVAddr = _kernelBaseAddress + pfnAddressOffset;
                    _dataProvider.ActiveAddressSpace = _kernelAddressSpace;
                    ulong? pfnPAddr = _dataProvider.ReadUInt64(pfnVAddr);
                    if (pfnPAddr == null)
                        return;
                    _pfnDatabaseBaseAddress = (ulong)pfnPAddr;
                    InfoHelper helper = new InfoHelper();
                    helper.Type = InfoHelperType.InfoDictionary;
                    helper.Name = _pfnDatabaseBaseAddress.ToString("X08");
                    helper.Title = "PFN Database Address";
                    helper.VirtualAddress = _pfnDatabaseBaseAddress;
                    helper.BufferSize = 4096; // just the first page, it's huge!
                    AddToInfoDictionary("PFN Database Address", helper);
                    _pfnDatabase = new PfnDatabase(_dataProvider, _profile, _pfnDatabaseBaseAddress);
                }
                catch
                {
                    return;
                }                
            });
        }
        async private Task ScanForDrivers()
        {
            await Task.Run(() =>
            {
                try
                {
                    DriverScan dScan = new DriverScan(_profile, _dataProvider);
                    DriverList = dScan.Run();
                    foreach (var item in _driverList)
                    {
                        Debug.WriteLine("  0x" + item.PhysicalAddress.ToString("x12").PadRight(14) +
                            item.DriverName.PadRight(30) +
                            item.Name.PadRight(22) +
                            item.DriverExtension.ServiceKeyName.PadRight(27) +
                            "0x" + item.DriverSize.ToString("x").PadRight(10) +
                            "0x" + item.DriverStartPointer.ToString("x12").PadRight(19) +
                            item.HandleCount.ToString().PadLeft(4) +
                            item.PointerCount.ToString().PadLeft(6)
                            );
                    }
                    NotifyPropertyChange("Drivers");
                }
                catch (Exception)
                {
                    return;
                }
            });
        }
        
        public void Dispose()
        {
            BigCleanUp();
        }
    }
}
