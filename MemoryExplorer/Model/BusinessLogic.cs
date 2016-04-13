using MemoryExplorer.Address;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

            IncrementActiveJobs("Finding Kernel DTB");
            await FindKernelDtb();
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
                p.ProcessName = "Idle";
                p.Pid = 0;
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
                    AddToInfoDictionary(friendlyKey, "0x" + friendlyValue.ToString("X08") + " (" + friendlyValue.ToString() + ")");
                }
            });
        }
        async private Task FindProfileGuid()
        {
            await Task.Run(() => 
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
                                    ProfileName = rsds.GuidAge + ".gz";
                                    AddToInfoDictionary("ProfileName", ProfileName);
                                    _profile = new Profile(ProfileName, @"E:\Forensics\MxProfileCache"); // TO DO - make this a user option when you get around to writing the settings dialog
                                    Architecture = _profile.Architecture;
                                    AddToInfoDictionary("Architecture", Architecture);
                                    if (_profile.Architecture == "I386")
                                        _kiUserSharedData = 0xFFDF0000;
                                    else
                                        _kiUserSharedData = 0xFFFFF78000000000;
                                    AddToInfoDictionary("KiUserSharedData", "0x" + _kiUserSharedData.ToString("X"));
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
            });
        }
        async private Task FindKernelDtb()
        {
            await Task.Run(() =>
            {
                // check if we already have it (from the live image)
                if (_kernelDtb != 0)
                    return;
                ulong filenameOffset = _profile.GetOffset("_EPROCESS", "ImageFileName");
                //ulong dtbOffset = _profile.GetOffset("_EPROCESS", "Pcb.DirectoryTableBase");
                //int dtbSize = _profile.GetSize("_EPROCESS", "Pcb.DirectoryTableBase");
                //long size = _profile.GetStructureSize("_EPROCESS");

                StringSearch mySearch = new StringSearch(_dataProvider);
                mySearch.AddNeedle("Idle\x00\x00\x00\x00\x00\x00\x00");
                //Dictionary<string, List<ulong>> results = mySearch.Scan();
                foreach (var answer in mySearch.Scan())
                {
                    try
                    {
                        List<ulong> hitList = answer.First().Value;
                        foreach (ulong hit in hitList)
                        {
                            try
                            {
                                EProcess ep = new EProcess(_profile, _dataProvider, hit - filenameOffset);
                                _kernelDtb = ep.DTB;
                                if(_kernelDtb > _dataProvider.ImageLength || _kernelDtb == 0)
                                {
                                    _kernelDtb = 0;
                                    continue;
                                }
                                if(ep.Pid != 0 || ep.Ppid != 0)
                                {
                                    _kernelDtb = 0;
                                    continue;
                                }
                                AddToInfoDictionary("Directory Table Base", "0x" + _kernelDtb.ToString("X08") + " (" + _kernelDtb.ToString() + ")");
                                AddToInfoDictionary("PID", ep.Pid.ToString());
                                AddToInfoDictionary("Parent PID", ep.Ppid.ToString());
                                return;
                            }
                            catch (Exception ex)
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
            });
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
                        AddToInfoDictionary("Build String", build);
                        try
                        {
                            uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                            pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset2, true);
                            if (pAddr == 0)
                                return;
                            buffer2 = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                            build = ReadString(buffer2, (uint)(pAddr & 0xfff));
                            AddToInfoDictionary("Build String Ex", build);
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
                                            CurrentHexViewerContentAddress = pAddr & 0xfffffffff000;
                                            CurrentHexViewerContent = buffer;
                                            string build = ReadString(buffer, (uint)(pAddr & 0xfff));
                                            AddToInfoDictionary("Kernel Base Address", "0x" + _kernelBaseAddress.ToString("X08"));
                                            AddToInfoDictionary("Build String", build);
                                            try
                                            {
                                                uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                                                pAddr = _kernelAddressSpace.vtop(_kernelBaseAddress + buildOffset2, true);
                                                if (pAddr == 0)
                                                    continue;
                                                buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                                build = ReadString(buffer, (uint)(pAddr & 0xfff));
                                                AddToInfoDictionary("Build String Ex", build);
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


                    AddToInfoDictionary("Version", version);
                    var pageCount = kusd.Get("NumberOfPhysicalPages");
                    AddToInfoDictionary("Physical Page Count", pageCount.ToString());
                    string rootDir = kusd.GetString("NtSystemRoot");
                    AddToInfoDictionary("System Root", rootDir);
                    var procCount = kusd.Get("ActiveProcessorCount");
                    AddToInfoDictionary("Active Processor Count", procCount.ToString());


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
                        ObjectTypeList = objectTypes.Records;
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
                    AddToInfoDictionary("PFN Database Address", _pfnDatabaseBaseAddress.ToString("X08"));
                    _pfnDatabase = new PfnDatabase(_dataProvider, _profile, _pfnDatabaseBaseAddress);
                }
                catch
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
