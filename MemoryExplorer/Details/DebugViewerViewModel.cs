using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Details
{
    public class DebugViewerViewModel : BindableBase
    {
        public string DebugTracer
        {
            get
            {
                string completeMessage = "";
                foreach (string message in _dataModel.DebugTracer)
                {
                    completeMessage += message;
                    completeMessage += "\n";
                }
                return completeMessage;
            }
        }
    }
}
