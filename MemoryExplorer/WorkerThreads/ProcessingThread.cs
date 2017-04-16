using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using MemoryExplorer.Worker;
using PluginContracts;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        #region globals
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private BlockingCollection<Job> _inbound = null;
        private BlockingCollection<Job> _outbound = null;
        private Assembly _pluginAssembly;
        private IProcessor _plugin;
        private DataModel _model;
        private DataProviderBase _dataProvider = null;
        private Profile _profile = null;
        #endregion

        public ProcessingThread(DataModel model)
        {
            _model = model;
            _profile = model.ActiveProfile;
            _dataProvider = model.DataProvider;
            _backgroundWorker.DoWork += new DoWorkEventHandler(ProcessingThread_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ProcessingThread_RunWorkerCompleted);
            _backgroundWorker.WorkerSupportsCancellation = true;
            // Start the asynchronous operation.
            _backgroundWorker.RunWorkerAsync();
        }
        public void Stop()
        {
            Debug.WriteLine("The processor is closing");
            _backgroundWorker.CancelAsync();
        }
        private void ProcessingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            _inbound = _model.ProcessorOut; // the models out is my in.
            _outbound = _model.ProcessorIn; // the models in is my out!

            foreach (var item in _inbound.GetConsumingEnumerable())
            {
                Job j = (Job)item;
                switch (j.Action)
                {
                    case JobAction.GetProfileIdentification:
                        GetProfileIdentifier(ref j);
                        break;
                    case JobAction.LoadPlugin:
                        LoadPlugin(ref j);
                        break;
                    case JobAction.LoadProfile:
                        LoadProfile(ref j);
                        break;
                    case JobAction.SetDataProvider:
                        SetDataProvider(ref j);
                        break;
                    case JobAction.FindKernelDtb:
                        FindIdleProcess(ref j);
                        break;
                    case JobAction.LoadKernelAddressSpace:
                        LoadKernelAddressSpace(ref j);
                        break;
                    case JobAction.FindKernelImage:
                        FindKernelImage(ref j);
                        break;
                    case JobAction.FindUserSharedData:
                        FindUserSharedData(ref j);
                        break;
                    case JobAction.EnumerateObjectTypes:
                        EnumerateObjectTypes(ref j);
                        break;
                    case JobAction.EnumerateObjectTree:
                        EnumerateObjectTree(ref j);
                        break;
                    default:
                        break;
                }
                _outbound.Add(j);

                if (worker.CancellationPending)
                    break;
            }
        }
        
        private void ProcessingThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
                //resultLabel.Text = "Canceled";
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                //resultLabel.Text = e.Result.ToString();
            }

            // Enable the UpDown control.
            //this.numericUpDown1.Enabled = true;

            // Enable the Start button.
            //startAsyncButton.Enabled = true;

            // Disable the Cancel button.
            //cancelAsyncButton.Enabled = false;
        }
        //private bool GetProfileConstant(string name, ref uint constant)
        //{
        //    if (_model.ProfileDll == null)
        //        return false;
        //    try
        //    {
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == @"LiveForensics.Symbols.MxSymbols")
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                var cst = c.LookupConstant(name);
        //                if (cst == null)
        //                    return false;
        //                constant = (uint)cst;
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private dynamic GetProfileCatalogueInfo()
        //{
        //    if (_model.ProfileDll == null)
        //        return null;
        //    try
        //    {
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == @"LiveForensics.Symbols.CatalogueInformation")
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                return c;
        //            }
        //        }
        //        return null;

        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //private dynamic GetProfileStructure(string name)
        //{
        //    if (_model.ProfileDll == null)
        //        return null;
        //    try
        //    {
        //        string target = "LiveForensics.Symbols." + name;
        //        foreach (Type type in _model.ProfileDll.GetExportedTypes())
        //        {
        //            if (type.FullName == target)
        //            {
        //                dynamic c = Activator.CreateInstance(type);
        //                return c;
        //            }
        //        }
        //        return null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
