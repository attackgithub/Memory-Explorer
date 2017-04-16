using MemoryExplorer.Address;
using MemoryExplorer.Worker;
using System;
using System.IO;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void LoadKernelAddressSpace(ref Job j)
        {
            ulong kernelDtb;

            try
            {
                // get the kernel dtb from the storage files
                string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1002.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    kernelDtb = ulong.Parse(items[1]);
                    if (_profile.Architecture == "I386")
                        _model.KernelAddressSpace = new AddressSpacex86Pae(_dataProvider, "idle", kernelDtb, true);
                    else
                        _model.KernelAddressSpace = new AddressSpacex64(_dataProvider, "idle", kernelDtb, true);
                    _model.ActiveAddressSpace = _model.KernelAddressSpace;
                }
                j.Status = JobStatus.Complete;
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                _model.KernelAddressSpace = null;
            }
        }
    }
}
