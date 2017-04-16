using MemoryExplorer.ModelObjects;
using MemoryExplorer.Worker;
using System;
using System.IO;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
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
                    if (items.Length == 4)
                    {
                        j.Status = JobStatus.Complete;
                        return;
                    }
                }


                ulong pAddr = _model.KernelAddressSpace.vtop(_model.KiUserSharedData);
                if (pAddr == 0)
                {
                    j.Status = JobStatus.Failed;
                    j.ErrorMessage = "coulnd't get VA for KiUserSharedData";
                    return;
                }
                KUserSharedData kusd = new KUserSharedData(pAddr, _profile);
                j.ActionMessage.Add(kusd.Version);
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
    }

}
