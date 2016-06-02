using MemoryExplorer.ModelObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Artifacts
{
    public class ObjectArtifact : ArtifactBase
    {
        private readonly ulong _physicalAddress;
        private readonly ulong _virtualAddress;
        private readonly ObjectHeader _objectHeader;

        public ObjectArtifact(ObjectHeader oh)
        {
            _virtualAddress = oh.VirtualAddress;
            _physicalAddress = oh.PhysicalAddress;
            _objectHeader = oh;
        }
        public ArtifactType ArtifactType
        {
            get { return ArtifactType.Object; }
        }
    }
}
