using MemoryExplorer.Processes;
using MemoryExplorer.Scanners;
using MemoryExplorer.Worker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
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
                    if (items.Length == 2)
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
                            Eprocess eproc = new Eprocess(_model, physicalAddress);
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
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
    }
}
