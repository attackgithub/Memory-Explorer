using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using MemoryExplorer.Worker;
using Pdb_Magician;
using PluginContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MemoryExplorer.WorkerThreads
{
    public class ProcessingThread
    {
        #region globals
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private Queue<Job> _inbound = null;
        private Queue<Job> _outbound = null;
        private Assembly _pluginAssembly;
        private IProcessor _plugin;
        private DataModel _model;
        private DataProviderBase _dataProvider = null;
        private AddressBase _kernelAddressSpace;
        private Profile _profile = null;
        #endregion

        public ProcessingThread(DataModel model)
        {
            _backgroundWorker.DoWork += new DoWorkEventHandler(ProcessingThread_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ProcessingThread_RunWorkerCompleted);
            _backgroundWorker.WorkerSupportsCancellation = true;
            // Start the asynchronous operation.
            _backgroundWorker.RunWorkerAsync(model);
        }
        public void Stop()
        {
            Debug.WriteLine("The processor is closing");
            _backgroundWorker.CancelAsync();
        }
        private void ProcessingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            _model = (DataModel)e.Argument;
            _inbound = _model.ProcessorOut; // the models out is my in.
            _outbound = _model.ProcessorIn; // the models in is my out!
            
            while (!worker.CancellationPending)
            {
                if (_inbound.Count > 0)
                {
                    Job j = _inbound.Dequeue();
                    switch (j.Action)
                    {
                        case JobAction.GetProfileIdentification:
                            GetProfileIdentifier(ref j);
                            break;
                        case JobAction.LoadPlugin:
                            LoadPlugin(ref j);
                            break;
                        case JobAction.LoadProfile:
                            LoadProfile(ref j);
                            break;
                        case JobAction.SetDataProvider:
                            SetDataProvider(ref j);
                            break;
                        case JobAction.FindKernelDtb:
                            FindIdleProcess(ref j);
                            break;
                        case JobAction.LoadKernelAddressSpace:
                            LoadKernelAddressSpace(ref j);
                            break;
                        case JobAction.FindKernelImage:
                            FindKernelImage(ref j);
                            break;
                        case JobAction.FindUserSharedData:
                            FindUserSharedData(ref j);
                            break;
                        default:
                            break;
                    }
                    _outbound.Enqueue(j);
                }
                else
                {
                    //Debug.WriteLine("The processor is waiting");
                    Thread.Sleep(500);
                }
            }
            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            //e.Result = ComputeFibonacci((int)e.Argument, worker, e);
        }

        private void FindUserSharedData(ref Job j)
        {
            try
            {
                j.ActionMessage.Clear();
                string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1004.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    if(items.Length == 4)
                    {
                        j.Status = JobStatus.Complete;
                        return;
                    }
                }


                ulong pAddr = _kernelAddressSpace.vtop(_model.KiUserSharedData);
                if (pAddr == 0)
                {
                    j.Status = JobStatus.Failed;
                    j.ErrorMessage = "coulnd't get VA for KiUserSharedData";
                    return;
                }
                KUserSharedData kusd = new KUserSharedData(pAddr, _profile);
                string version = VersionHelper(kusd.Version);
                j.ActionMessage.Add(version);
                uint npp = kusd.NumberOfPhysicalPages;
                j.ActionMessage.Add(npp.ToString());
                string nsr = kusd.NtSystemRoot;
                j.ActionMessage.Add(nsr);
                UInt64 ticks = kusd.SystemTime;
                j.ActionMessage.Add(ticks.ToString());
                File.WriteAllLines(Path.Combine(_dataProvider.CacheFolder, "1004.dat"), j.ActionMessage);
                j.Status = JobStatus.Complete;
                return;
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                return;
            }
        }
        private string VersionHelper(string version)
        {
            switch (version)
            {
                case "10.0":
                    return "10.0 (Windows 10)";
                case "6.3":
                    return "6.3 (Windows 8.1 or 2012 R2)";
                case "6.2":
                    return "6.2 (Windows 8 or 2012)";
                case "6.1":
                    return "6.1 (Windows 7 or 2008 R2)";
                case "6.0":
                    return "6.0 (Windows Vista or 2008)";
                case "5.2":
                    return "5.2 (Windows XP x64 or 2003 or 2003 R2)";
                case "5.1":
                    return "5.1 (Windows XP)";
                case "5.0":
                    return "5.0 (Windows 2000)";
                default:
                    return version;
            }
        }
        private void FindKernelImage(ref Job j)
        {
            ulong kernelBaseAddress = 0;
            string buildString;
            string buildStringEx;

            try
            {
                j.ActionMessage.Clear();
                string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1003.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    foreach(string s in items)
                    {
                        j.ActionMessage.Add(s);
                    }
                    j.Status = JobStatus.Complete;
                    return;
                }

                uint buildOffset = 0;
                try
                {
                    buildOffset = (uint)_profile.GetConstant("NtBuildLab");
                }
                catch
                {
                    buildOffset = (uint)_profile.GetConstant("_NtBuildLab");
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
                                if (sig == "MZ")
                                {
                                    PE peHeader = new PE(_dataProvider, _kernelAddressSpace, page);
                                    RSDS debugSection = peHeader.DebugSection;
                                    if (IsValidKernel(debugSection.Filename))
                                    {
                                        kernelBaseAddress = page;
                                        j.ActionMessage.Add(kernelBaseAddress.ToString());
                                        ulong pAddr = _kernelAddressSpace.vtop(kernelBaseAddress + buildOffset, false);
                                        if (pAddr == 0)
                                            continue;
                                        buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                        buildString = ReadString(buffer, (uint)(pAddr & 0xfff));
                                        j.ActionMessage.Add(buildString);
                                        try
                                        {
                                            uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                                            pAddr = _kernelAddressSpace.vtop(kernelBaseAddress + buildOffset2, true);
                                            if (pAddr == 0)
                                                continue;
                                            buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                            buildStringEx = ReadString(buffer, (uint)(pAddr & 0xfff));
                                            j.ActionMessage.Add(buildStringEx);
                                        }
                                        catch { }
                                        j.Status = JobStatus.Complete;
                                        File.WriteAllLines(Path.Combine(_dataProvider.CacheFolder, "1003.dat"), j.ActionMessage);
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
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                return;
            }
            j.Status = JobStatus.Failed;
            j.ErrorMessage = "Couldn't match signatures for kernel image";
        }
        private string ReadString(byte[] buffer, uint offset)
        {
            string result = "";
            while (buffer[offset] != 0)
            {
                result += (char)buffer[offset];
                offset++;
            }
            return result;
        }
        private bool IsValidKernel(string name)
        {
            List<string> KernelNames = new List<string>();
            KernelNames.Add("ntkrnlmp.pdb");
            KernelNames.Add("ntkrnlpa.pdb");
            KernelNames.Add("ntoskrnl.pdb");
            KernelNames.Add("ntkrpamp.pdb");

            return KernelNames.Contains(name);
        }
        private void LoadKernelAddressSpace(ref Job j)
        {
            try
            {
                if (_profile.Architecture == "I386")
                    _model.KernelAddressSpace = new AddressSpacex86Pae(_dataProvider, "idle", _model.KernelDtb, true);
                else
                    _model.KernelAddressSpace = new AddressSpacex64(_dataProvider, "idle", _model.KernelDtb, true);
                _profile.KernelAddressSpace = _model.KernelAddressSpace;
                _dataProvider.ActiveAddressSpace = _model.KernelAddressSpace;
                j.Status = JobStatus.Complete;
                _kernelAddressSpace = _model.KernelAddressSpace;
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                _model.KernelAddressSpace = null;
            }
        }

        /// <summary>
        /// Find the Idle process in memory and at the same time
        /// make a note of the Directory Table Base (DTB)
        /// </summary>
        /// <param name="j"></param>
        /// <returns>PhysicalAddress of the Idle process</returns>
        private void FindIdleProcess(ref Job j)
        {
            // TODO - this function could find more than one hit, currently I'm stopping at the first one.
            bool escape = false;
            ulong physicalAddress = 0;
            j.ActionMessage.Clear();
            string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1002.dat");
            // have we already got the answer?
            try
            {
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    if(items.Length == 2)
                    {
                        //physicalAddress = ulong.Parse(items[0]);
                        //_model.KernelDtb = ulong.Parse(items[1]);
                        //j.ActionMessage.Add(items[0]);
                        //j.ActionMessage.Add(items[1]);
                        j.Status = JobStatus.Complete;
                        return;
                    }
                }
            }
            catch
            {
                // something went wrong when reading the archive, so let's just delete it.
                File.Delete(archiveFile);
            }
            try
            {
                // check if we already have it (from the live image)
                if (_model.KernelDtb == 0)
                {
                    uint filenameOffset = _profile.GetMemberOffset("_EPROCESS", "ImageFileName"); // Pcb.Flags.ExecuteDisable
                    StringSearch mySearch = new StringSearch(_dataProvider);
                    mySearch.AddNeedle("Idle\x00\x00\x00\x00\x00\x00\x00");
                    foreach (var answer in mySearch.Scan())
                    {
                        if (escape)
                            break;
                        List<ulong> hitList = answer.First().Value;
                        foreach (ulong hit in hitList)
                        {
                            physicalAddress = hit - filenameOffset;
                            //dynamic ep = _model.ActiveProfile.GetStructure("_EPROCESS", physicalAddress);
                            Eprocess eproc = new Eprocess(physicalAddress, _model.ActiveProfile);
                            _model.KernelDtb = eproc.DTB;
                            if (_model.KernelDtb > _dataProvider.ImageLength || _model.KernelDtb == 0)
                            {
                                _model.KernelDtb = 0;
                                physicalAddress = 0;
                                continue;
                            }
                            if (eproc.Pid != 0 || eproc.Ppid != 0)
                            {
                                _model.KernelDtb = 0;
                                physicalAddress = 0;
                                continue;
                            }
                            escape = true;
                            break;
                        }
                    }
                }
                if (physicalAddress == 0)
                {
                    j.Status = JobStatus.Failed;
                    j.ErrorMessage = "Couldn't Find the Idle Process";
                }
                else
                {
                    j.ActionMessage.Add(physicalAddress.ToString());
                    j.ActionMessage.Add(_model.KernelDtb.ToString());
                    j.Status = JobStatus.Complete;
                    File.WriteAllLines(Path.Combine(_dataProvider.CacheFolder, "1002.dat"), j.ActionMessage);
                }
            }
            catch(Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }    
        private void LoadProfile(ref Job j)
        {
            try
            {
                string targetProfile = Path.Combine(Path.Combine(_model.ProfileCacheLocation, j.ActionMessage[0]), "LiveForensics.Symbols.dll");
                if (new FileInfo(targetProfile).Exists)
                {
                    _profile = new Profile(targetProfile, _dataProvider, _model);
                    if(_profile.Architecture == "I386")
                        _model.KiUserSharedData = 0xFFDF0000;
                    else if (_profile.Architecture == "AMD64")
                        _model.KiUserSharedData = 0xFFFFF78000000000;
                    _model.ActiveProfile = _profile;
                    j.Status = JobStatus.Complete;
                }
                else
                {
                    _model.ActiveProfile = null;
                    _profile = null;
                    j.Status = JobStatus.Failed;
                    j.ErrorMessage = "Failed to load requested profile: " + targetProfile;
                }
            }
            catch(Exception ex)
            {
                _model.ActiveProfile = null;
                _profile = null;
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
        private void SetDataProvider(ref Job j)
        {
            string targetImage = _model.MemoryImageFilename;
            string ImageMd5 = GetMD5HashFromFile(targetImage);
            FileInfo fi = new FileInfo(targetImage);
            string cacheLocation = fi.Directory.FullName + "\\[" + fi.Name + "]" + ImageMd5;
            DirectoryInfo di = new DirectoryInfo(cacheLocation);
            if (!di.Exists)
                di.Create();
            _dataProvider = new ImageDataProvider(_model, cacheLocation);
            j.ActionMessage.Clear();
            j.Status = JobStatus.Complete;
            j.ActionMessage.Add(cacheLocation);
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
        private void GetProfileIdentifier(ref Job j)
        {
            bool missingProfile = false;
            string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1001.dat");
            FileInfo fi = new FileInfo(archiveFile);
            if(fi.Exists)
            {
                string[] items = File.ReadAllLines(archiveFile);
                foreach(string item in items)
                {
                    j.ActionMessage.Add(item);
                    string profileCache = Path.Combine(_model.ProfileCacheLocation, item);
                    if(!new DirectoryInfo(profileCache).Exists)
                    {
                        missingProfile = true;
                    }
                }
                if(!missingProfile)
                {
                    j.Status = JobStatus.Complete;
                    return;
                }
            }
            StringSearch mySearch = new StringSearch(_dataProvider);
            PdbMagician magician = new PdbMagician();
            List<string> todoList = new List<string>();
            todoList.Add("_EPROCESS");
            todoList.Add("_KUSER_SHARED_DATA");
            int successCount = 0;
            mySearch.AddNeedle("RSDS");
            foreach (var answer in mySearch.Scan())
            {
                try
                {
                    List<ulong> hitList = answer["RSDS"];
                    foreach (ulong hit in hitList)
                    {
                        RSDS rsds = new RSDS(_dataProvider, hit);
                        if (rsds.Signature == "RSDS" && (rsds.Filename == "ntkrnlpa.pdb" || rsds.Filename == "ntkrnlmp.pdb" || rsds.Filename == "ntkrpamp.pdb" || rsds.Filename == "ntoskrnl.pdb"))
                        {
                            
                            string profileCache = Path.Combine(_model.ProfileCacheLocation, rsds.GuidAge);
                            DirectoryInfo di = new DirectoryInfo(profileCache);
                            if (!di.Exists)
                            {
                                bool result = magician.RetrieveSymbolFile(rsds.Filename, rsds.GuidAge, _model.ProfileCacheLocation);
                                string targetPdb = Path.Combine(Path.Combine(_model.ProfileCacheLocation, rsds.GuidAge), rsds.Filename);
                                fi = new FileInfo(targetPdb);
                                if (result && fi.Exists)
                                {
                                    result = magician.ParseSymbolFile(targetPdb, profileCache, todoList.ToArray());
                                    if (result && !j.ActionMessage.Contains(rsds.GuidAge))
                                    {
                                        successCount++;
                                        j.ActionMessage.Add(rsds.GuidAge);
                                    }
                                }
                            }
                            else if (!j.ActionMessage.Contains(rsds.GuidAge))
                            {
                                successCount++;
                                j.ActionMessage.Add(rsds.GuidAge);
                            }
                            Debug.WriteLine(hit.ToString("X8") + "\t" + rsds.Filename + "\t" + rsds.GuidAge);
                        }
                    }
                    
                }
                catch { }
            }
            if (successCount > 0)
            {
                j.Status = JobStatus.Complete;
                File.WriteAllLines(Path.Combine(_dataProvider.CacheFolder, "1001.dat"), j.ActionMessage);                
            }
            else
                j.Status = JobStatus.Failed;

            
            //Dictionary<string, object> info = _dataProvider.GetInformation();
            //string friendlyKey;
            //foreach (var item in info)
            //{
            //    if (item.Key == "dtb")
            //    {
            //        friendlyKey = "Directory Table Base";
            //        //_model._kernelDtb = (ulong)item.Value;
            //    }
            //    else if (item.Key == "maximumPhysicalAddress")
            //        friendlyKey = "Maximum Physical Address";
            //}
        }
        private void LoadPlugin(ref Job j)
        {
            try
            {
                _plugin = null;
                var pluginLocation = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                Debug.WriteLine("The processor is loading plugin " + j.ActionMessage[0]);
                AssemblyName an = AssemblyName.GetAssemblyName(Path.Combine(pluginLocation, j.ActionMessage[0] + ".dll"));
                Debug.WriteLine("Plugin: " + an.FullName);
                _pluginAssembly = Assembly.Load(an);
                Type[] types = _pluginAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsInterface || type.IsAbstract) { continue; }
                    if (type.FullName == "LocalProcessor.Processor") // have have an interface compatible plugin
                    {
                        _plugin = Activator.CreateInstance(type) as IProcessor;
                        if(_plugin != null)
                        {
                            j.Status = JobStatus.Complete;
                            //_outbound.Enqueue(j);
                            Debug.WriteLine("Loaded Plugin Says: " + _plugin.Name);
                        }
                        else
                        {
                            j.Status = JobStatus.Failed;
                            j.ErrorMessage = "Incompatible Plugin";
                            //_outbound.Enqueue(j);
                        }
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                //_outbound.Enqueue(j);
            }            
        }
        private void ProcessingThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
                //resultLabel.Text = "Canceled";
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                //resultLabel.Text = e.Result.ToString();
            }

            // Enable the UpDown control.
            //this.numericUpDown1.Enabled = true;

            // Enable the Start button.
            //startAsyncButton.Enabled = true;

            // Disable the Cancel button.
            //cancelAsyncButton.Enabled = false;
        }
        //private bool GetProfileConstant(string name, ref uint constant)
        //{
        //    if (_model.ProfileDll == null)
        //        return false;
        //    try
        //    {
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == @"LiveForensics.Symbols.MxSymbols")
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                var cst = c.LookupConstant(name);
        //                if (cst == null)
        //                    return false;
        //                constant = (uint)cst;
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private dynamic GetProfileCatalogueInfo()
        //{
        //    if (_model.ProfileDll == null)
        //        return null;
        //    try
        //    {
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == @"LiveForensics.Symbols.CatalogueInformation")
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                return c;
        //            }
        //        }
        //        return null;

        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //private dynamic GetProfileStructure(string name)
        //{
        //    if (_model.ProfileDll == null)
        //        return null;
        //    try
        //    {
        //        string target = "LiveForensics.Symbols." + name;
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == target)
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                return c;
        //            }
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
