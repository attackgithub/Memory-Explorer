using MemoryExplorer.Processes;
using System;
using System.Collections.Generic;

namespace MemoryExplorer.Worker
{
    public class Job
    {
        public ProcessInfo ProcessInformation;
        public JobStatus Status;
        public JobAction Action;
        public string ErrorMessage;
        public List<string> ActionMessage;
        public long JobNumber;

        public Job()
        {
            ProcessInformation = null;
            Status = JobStatus.Unknown;
            ErrorMessage = "";
            ActionMessage = new List<string>();
            Action = JobAction.Unknown;
            JobNumber = DateTime.Now.Ticks;
        }
        public Job(ProcessInfo processInformation, JobStatus status=JobStatus.Unknown)
        {
            ProcessInformation = processInformation;
            Status = status;
            ErrorMessage = "";
            ActionMessage = new List<string>();
            Action = JobAction.Unknown;
            JobNumber = DateTime.Now.Ticks;
        }
    }
}
