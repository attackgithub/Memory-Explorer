using MemoryExplorer.Address;
using MemoryExplorer.Model;
using MemoryExplorer.Worker;
using PluginContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace MemoryExplorer.WorkerThreads
{
    public class IngesterThread
    {
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private Queue<Job> _inbound = null;
        private Queue<Job> _outbound = null;
        private Assembly _pluginAssembly;
        private IIngester _plugin;
        private string _cacheFolder;
        private DataModel _model;

        public IngesterThread(DataModel model)
        {
            _backgroundWorker.DoWork += new DoWorkEventHandler(IngestingThread_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(IngestingThread_RunWorkerCompleted);
            _backgroundWorker.WorkerSupportsCancellation = true;

            // Start the asynchronous operation.
            _backgroundWorker.RunWorkerAsync(model);
        }
        public void Stop()
        {
            Debug.WriteLine("The ingester is closing");

            _backgroundWorker.CancelAsync();
        }
        private void IngestingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            _model = (DataModel)e.Argument;
            _inbound = _model.IngesterOut; // the models out is my in.
            _outbound = _model.IngesterIn; // the models in is my out!

            while (!worker.CancellationPending)
            {
                if (_inbound.Count > 0)
                {
                    Job j = _inbound.Dequeue();
                    switch (j.Action)
                    {
                        case JobAction.SetCacheFolder:
                            _cacheFolder = j.ActionMessage[0];
                            break;
                        case JobAction.LoadPlugin:
                            LoadPlugin(j);
                            break;
                        case JobAction.LoadProfileId:
                            LoadProfileId(ref j);
                            break;
                        case JobAction.FindKernelDtb:
                            FindKernelDtb(ref j);
                            break;
                        case JobAction.FindKernelImage:
                            FindKernelImage(ref j);
                            break;
                        case JobAction.FindUserSharedData:
                            FindUserSharedData(ref j);
                            break;
                        case JobAction.LoadKernelAddressSpace:
                            LoadKernelAddressSpace(ref j);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    //Debug.WriteLine("The ingester is waiting");
                    Thread.Sleep(15);
                }
            }
            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            //e.Result = ComputeFibonacci((int)e.Argument, worker, e);
        }

        private void LoadKernelAddressSpace(ref Job j)
        {
            try
            {
                // get the kernel dtb from the storage files
                string archiveFile = Path.Combine(_model.DataProvider.CacheFolder, "1002.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    ulong kernelDtb = ulong.Parse(items[1]);
                    if (_model.ActiveProfile.Architecture == "I386")
                        _model.KernelAddressSpace = new AddressSpacex86Pae(_model.DataProvider, "idle", kernelDtb, true);
                    else
                        _model.KernelAddressSpace = new AddressSpacex64(_model.DataProvider, "idle", kernelDtb, true);
                }
            }
            catch (Exception ex)
            {
                // message box warning
                _model.KernelAddressSpace = null;
            }
            
        }

        private void FindUserSharedData(ref Job j)
        {
            try
            {
                string archiveFile = Path.Combine(_cacheFolder, "1004.dat");
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void FindKernelDtb(ref Job j)
        {
            try
            {
                string archiveFile = Path.Combine(_cacheFolder, "1002.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    // item[0] is the physical address of the idle eprocess structure
                    _model.KernelDtb = ulong.Parse(items[1]);
                }
            }
            catch { }                            
        }

        private void FindKernelImage(ref Job j)
        {
            string buildString;
            string buildStringEx;

            try
            {
                string archiveFile = Path.Combine(_cacheFolder, "1003.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(archiveFile);
                    _model.KernelBaseAddress = ulong.Parse(items[0]);
                    // TODO use the following strings in a infohelper object
                    if (items.Length > 1)
                        buildString = items[1];
                    if (items.Length > 2)
                        buildStringEx = items[2];
                }

            }
            catch { }
        }

        private void LoadProfileId(ref Job j)
        {
            string archiveFile = Path.Combine(_cacheFolder, "1001.dat");
            string[] items = File.ReadAllLines(archiveFile);


        }

        private void LoadPlugin(Job j)
        {
            try
            {
                _plugin = null;
                var pluginLocation = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                Debug.WriteLine("The ingester is loading plugin " + j.ActionMessage[0]);
                AssemblyName an = AssemblyName.GetAssemblyName(Path.Combine(pluginLocation, j.ActionMessage[0] + ".dll"));
                Debug.WriteLine("Plugin: " + an.FullName);
                _pluginAssembly = Assembly.Load(an);
                Type[] types = _pluginAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsInterface || type.IsAbstract) { continue; }
                    if (type.FullName == "LocalIngester.Ingester") // have have an interface compatible plugin
                    {
                        _plugin = Activator.CreateInstance(type) as IIngester;
                        if (_plugin != null)
                        {
                            j.Status = JobStatus.Complete;
                            _outbound.Enqueue(j);
                            Debug.WriteLine("Loaded Plugin Says: " + _plugin.Name);
                        }
                        else
                        {
                            j.Status = JobStatus.Failed;
                            j.ErrorMessage = "Incompatible Plugin";
                            _outbound.Enqueue(j);
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                _outbound.Enqueue(j);
            }
        }
        private void IngestingThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
    }
}
