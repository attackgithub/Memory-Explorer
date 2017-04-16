using System;
using System.ComponentModel;
using System.Diagnostics;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged, IDisposable
    {
        ~DataModel()
        {
            try
            {
                Debug.WriteLine("Cleanup Called");
                _processorOut.CompleteAdding();
                _ingesterOut.CompleteAdding();
                if (_processingThread != null)
                    _processingThread.Stop();
                if (_ingestingThread != null)
                    _ingestingThread.Stop();
                if (_queueManagerThread != null)
                    _queueManagerThread.Stop();
            }
            catch { }
        }
    }
}
