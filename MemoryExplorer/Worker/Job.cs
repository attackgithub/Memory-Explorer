using MemoryExplorer.Processes;

namespace MemoryExplorer.Worker
{
    public class Job
    {
        public ProcessInfo ProcessInformation;
        public JobStatus Status;

        public Job()
        {
            ProcessInformation = null;
            Status = JobStatus.Unknown;
        }
        public Job(ProcessInfo processInformation, JobStatus status=JobStatus.Unknown)
        {
            ProcessInformation = processInformation;
            Status = status;
        }
    }
}
