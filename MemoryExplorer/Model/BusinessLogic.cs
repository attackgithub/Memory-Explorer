using MemoryExplorer.Address;
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
    public partial class DataModel : INotifyPropertyChanged
    {
        async private void InitialSurvey()
        {
            IncrementActiveJobs();
            await GetInformation();
            DecrementActiveJobs();

            IncrementActiveJobs();
            await FindProfileGuid();
            DecrementActiveJobs();

            if (_profile == null)
                return;

            IncrementActiveJobs();
            await FindKernelDtb();
            DecrementActiveJobs();

            if (_kernelDtb == 0)
                return;

            IncrementActiveJobs();
            AddressBase addr = await LoadKernelAddressSpace();
            if(addr != null)
            {
                ProcessInfo p = new ProcessInfo();
                p.AddressSpace = addr;
                p.ProcessName = "Idle";
                p.Pid = 0;
                AddProcess(p);
            }
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
                        friendlyKey = "Kernel Base Address";
                    else if (item.Key == "kdbg")
                        friendlyKey = "KDBG";
                    else if (item.Key == "pfnDatabase")
                        friendlyKey = "PFN Database Address";
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
    }
}
