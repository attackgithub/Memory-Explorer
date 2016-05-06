using MemoryExplorer.Processes;

namespace MemoryExplorer.Worker
{
    public class Job
    {
        public ProcessInfo ProcessInformation;
        public JobStatus Status;
        public string ErrorMessage;

        public Job()
        {
            ProcessInformation = null;
            Status = JobStatus.Unknown;
            ErrorMessage = "";
        }
        public Job(ProcessInfo processInformation, JobStatus status=JobStatus.Unknown)
        {
            ProcessInformation = processInformation;
            Status = status;
        }
    }
}
