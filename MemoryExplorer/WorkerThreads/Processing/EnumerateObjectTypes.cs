using MemoryExplorer.Details;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Worker;
using System;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void EnumerateObjectTypes(ref Job j)
        {
            try
            {
                j.ActionMessage.Clear();
                MxObjectTypes objectTypes = new MxObjectTypes(_model);
                j.Status = JobStatus.Complete;
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
    }
}
