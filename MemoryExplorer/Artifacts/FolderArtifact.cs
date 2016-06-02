using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Artifacts
{
    public class FolderArtifact : ArtifactBase
    {
        public ArtifactType ArtifactType
        {
            get { return ArtifactType.Folder; }
        }
    }
}
