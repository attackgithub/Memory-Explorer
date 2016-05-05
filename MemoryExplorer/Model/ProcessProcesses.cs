using MemoryExplorer.Processes;
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
            Random r = new Random();
            int rnd = r.Next(1500, 5500);
            Thread.Sleep(rnd);
            e.Result = job;
        }
        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Job job = e.Result as Job;
            AddDebugMessage("Background Worker Finished: " + job.ProcessInformation.ProcessName);
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
