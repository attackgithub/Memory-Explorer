using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using MemoryExplorer.Worker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
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
                    foreach (string s in items)
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
                            ulong vAddr = _model.KernelAddressSpace.ptov(hit);
                            if (vAddr == 0)
                                continue;
                            //// let's grab the PE header while we're here
                            ulong page = vAddr & 0xfffffffff000;
                            // remember PE images are page aligned
                            for (int i = 0; i < 10; i++) // need to think about 10 being enough
                            {
                                ulong tryAddress = _model.KernelAddressSpace.vtop(page, false);
                                buffer = _dataProvider.ReadMemory(tryAddress, 1);
                                string sig = Encoding.Default.GetString(buffer, 0, 2);
                                if (sig == "MZ")
                                {
                                    PE peHeader = new PE(_dataProvider, _model.KernelAddressSpace, page);
                                    RSDS debugSection = peHeader.DebugSection;
                                    if (IsValidKernel(debugSection.Filename))
                                    {
                                        kernelBaseAddress = page;
                                        j.ActionMessage.Add(kernelBaseAddress.ToString());
                                        ulong pAddr = _model.KernelAddressSpace.vtop(kernelBaseAddress + buildOffset, false);
                                        if (pAddr == 0)
                                            continue;
                                        buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                        buildString = ReadString(buffer, (uint)(pAddr & 0xfff));
                                        j.ActionMessage.Add(buildString);
                                        try
                                        {
                                            uint buildOffset2 = (uint)_profile.GetConstant("NtBuildLabEx");
                                            pAddr = _model.KernelAddressSpace.vtop(kernelBaseAddress + buildOffset2, true);
                                            if (pAddr == 0)
                                                continue;
                                            buffer = _dataProvider.ReadMemory(pAddr & 0xfffffffff000, 2);
                                            buildStringEx = ReadString(buffer, (uint)(pAddr & 0xfff));
                                            j.ActionMessage.Add(buildStringEx);
                                        }
                                        catch { }
                                        _model.KernelBaseAddress = kernelBaseAddress;
                                        _model.ActiveAddressSpace = _model.KernelAddressSpace;
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
        private bool IsValidKernel(string name)
        {
            List<string> KernelNames = new List<string>();
            KernelNames.Add("ntkrnlmp.pdb");
            KernelNames.Add("ntkrnlpa.pdb");
            KernelNames.Add("ntoskrnl.pdb");
            KernelNames.Add("ntkrpamp.pdb");

            return KernelNames.Contains(name);
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
    }
}
