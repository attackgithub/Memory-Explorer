using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Tools;
using MemoryExplorer.Worker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged
    {
        private int _coreCount = 0;
        private List<BackgroundWorker> _workerPool = new List<BackgroundWorker>();
        private Queue<Job> _waitingJobs = new Queue<Job>();
        private volatile bool _shouldStop;

        /// <summary>
        /// OK, this is the mother of all processing
        /// We're going to see how many processors we have available
        /// We're going to create a pool of background workers, 1 per processor
        /// We're going to allocate individual processes to the background workers as they become free
        /// 
        /// </summary>
        async private void ProcessProcesses()
        {
            IncrementActiveJobs("Processes");
            // how many processors do we have available?
            _coreCount = Environment.ProcessorCount;
            AddDebugMessage("Host Reported Core Count: " + _coreCount.ToString());
            _coreCount = 1;
            // create the worker pool
            for (int i = 0; i < _coreCount; i++)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = false;
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += new DoWorkEventHandler(ProcessAProcess);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
                //bw.ProgressChanged += new ProgressChangedEventHandler(Vpe_ProgressChanged);
                _workerPool.Add(bw);
            }
            // create one job per process
            foreach (ProcessInfo info in _processList)
            {
                Job j = new Job(info, JobStatus.Queued);
                _waitingJobs.Enqueue(j);
            }
            // start giving jobs to the worker pool
            await Task.Run(() =>
            {
                _shouldStop = false;
                while (!_shouldStop)
                {
                    if (_waitingJobs.Count > 0)
                    {
                        ProcessJob();
                    }    
                    else
                        _shouldStop = true;
                }
            });
            DecrementActiveJobs();
        }
        private void ProcessJob()
        {
            int worker = FindFreeWorker();
            if (worker == -1)
            {
                Thread.Sleep(10); // dunno, should I?
                return;
            }
            Job currentJob = _waitingJobs.Dequeue();
            currentJob.Status = JobStatus.Running;
            _workerPool[worker].RunWorkerAsync(currentJob);
        }
        private void ProcessAProcess(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Job job = e.Argument as Job;
            AddDebugMessage("Background Worker: " + job.ProcessInformation.ProcessName);
            // get the EPROCESS object
            EProcess_deprecated ep = null;
            // pid 0 is special because it's the Idle process and has no virtual address
            lock(_profile.AccessLock)
            {
                if (job.ProcessInformation.Pid == 0)
                    ep = new EProcess_deprecated(this, physicalAddress: job.ProcessInformation.PhysicalAddress);
                else
                    ep = new EProcess_deprecated(this, job.ProcessInformation.VirtualAddress);
            }
            if (ep.Pid != job.ProcessInformation.Pid)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = "EPROCESS Virtual Address resulted in a different PID to the Process Pid";
                e.Result = job;
                return;
            }
            job.ProcessInformation.HandleTableAddress = ep.ObjectTable;

            //lock (_profile.AccessLock)
            {
                Handles handles = new Handles(this, job.ProcessInformation.Pid, job.ProcessInformation.HandleTableAddress);
                job.ProcessInformation.HandleTable = handles.Run();
            }
            if(job.ProcessInformation.HandleTable != null && job.ProcessInformation.HandleTable.Count > 0)
            {
                ProcessHandleTable(job.ProcessInformation);
            }

            job.Status = JobStatus.Complete;
            if(job.ProcessInformation.HandleTable == null)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = "Process Had No Handles";
            }
            e.Result = job;
        }

        private void ProcessHandleTable(ProcessInfo processInformation)
        {
            List<HandleRecord> handleRecords = new List<HandleRecord>();
            foreach (HandleTableEntry e in processInformation.HandleTable)
            {
                HandleRecord record = new HandleRecord();
                //lock (_profile.AccessLock)
                {
                    record.objectHeader = new ObjectHeader(this, virtualAddress: e.ObjectPointer);
                }
                if (record.objectHeader.HeaderNameInfo != null)
                    record.Name = record.objectHeader.HeaderNameInfo.Name;
                string objectName = GetObjectName(e.TypeInfo);
                switch(objectName)
                {
                    case "File":
                        break;
                    case "Key":
                        //lock (_profile.AccessLock)
                        {
                            RegistryKey rk = new RegistryKey(this, record.objectHeader, (e.ObjectPointer + (ulong)record.objectHeader.Size));
                            record.Details = rk.Name;
                        }
                        break;
                    case "Thread":
                        break;
                    case "Process":
                        break;

                    default:
                        break;
                }

                handleRecords.Add(record);
            }
            processInformation.HandleRecords = handleRecords;
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Job job = e.Result as Job;
            if (job.Status == JobStatus.Complete)
                AddDebugMessage("Background Worker Finished: " + job.ProcessInformation.ProcessName + " Handles: " + job.ProcessInformation.HandleTable.Count.ToString());
            else
                AddDebugMessage("Background Worker ERROR: " + job.ProcessInformation.ProcessName + " - " + job.ErrorMessage);
        }
        private void KillBackgroundWorkerPool()
        {
            foreach (BackgroundWorker bw in _workerPool)
            {
                bw.CancelAsync();
            }
        }
        private int FindFreeWorker()
        {
            for (int i = 0; i < _coreCount; i++)
            {
                if (!_workerPool[i].IsBusy)
                    return i;
            }
            return -1;
        }
    }
}
