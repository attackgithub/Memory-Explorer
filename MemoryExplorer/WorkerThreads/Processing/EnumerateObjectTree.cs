using MemoryExplorer.Tools;
using MemoryExplorer.Worker;
using System;
using System.Collections.Generic;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void EnumerateObjectTree(ref Job j)
        {
            try
            {
                ObjectTree ot = new ObjectTree(_model);
                //List<ObjectTreeRecord> records = ot.Run();
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
    }
}
