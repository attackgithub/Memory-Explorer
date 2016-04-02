using MemoryExplorer.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Artifacts
{
    public class ProcessArtifact : ArtifactBase
    {
        private ProcessInfo _process;

        public ArtifactType ArtifactType
        {
            get { return ArtifactType.Process; }
        }

        public ProcessInfo LinkedProcess
        {
            get
            {
                return _process;
            }

            set
            {
                _process = value;
            }
        }
    }
}
