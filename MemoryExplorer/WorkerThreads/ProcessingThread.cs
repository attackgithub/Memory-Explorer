using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Worker;
using PluginContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MemoryExplorer.WorkerThreads
{
    public class ProcessingThread
    {
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private Queue<Job> _inbound = null;
        private Queue<Job> _outbound = null;
        private Assembly _pluginAssembly;
        private IProcessor _plugin;
        private DataModel _model;
        private DataProviderBase _dataProvider = null;

        public ProcessingThread(DataModel model)
        {
            _backgroundWorker.DoWork += new DoWorkEventHandler(ProcessingThread_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ProcessingThread_RunWorkerCompleted);
            _backgroundWorker.WorkerSupportsCancellation = true;
            // Start the asynchronous operation.
            _backgroundWorker.RunWorkerAsync(model);
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
            _model = (DataModel)e.Argument;
            _inbound = _model.ProcessorOut; // the models out is my in.
            _outbound = _model.ProcessorIn; // the models in is my out!
            
            while (!worker.CancellationPending)
            {
                if (_inbound.Count > 0)
                {
                    Job j = _inbound.Dequeue();
                    switch (j.Action)
                    {
                        case JobAction.LoadPlugin:
                            LoadPlugin(j);
                            break;
                        case JobAction.GetInformation:
                            GetInformation(j);
                            break;
                        case JobAction.SetDataProvider:
                            SetDataProvider(j);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine("The processor is waiting");
                    Thread.Sleep(500);
                }
            }
            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            //e.Result = ComputeFibonacci((int)e.Argument, worker, e);
        }

        private void SetDataProvider(Job j)
        {
            string targetImage = _model.MemoryImageFilename;
            string ImageMd5 = GetMD5HashFromFile(targetImage);
            FileInfo fi = new FileInfo(targetImage);
            string cacheLocation = fi.Directory.FullName + "\\[" + fi.Name + "]" + ImageMd5;
            DirectoryInfo di = new DirectoryInfo(cacheLocation);
            if (!di.Exists)
                di.Create();
            _dataProvider = new ImageDataProvider(_model, cacheLocation);
        }
        private string GetMD5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        private void GetInformation(Job j)
        {
            Dictionary<string, object> info = _dataProvider.GetInformation();
            string friendlyKey;
            foreach (var item in info)
            {
                if (item.Key == "dtb")
                {
                    friendlyKey = "Directory Table Base";
                    //_model._kernelDtb = (ulong)item.Value;
                }
                else if (item.Key == "maximumPhysicalAddress")
                    friendlyKey = "Maximum Physical Address";
            }
        }

        private void LoadPlugin(Job j)
        {
            try
            {
                _plugin = null;
                var pluginLocation = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                Debug.WriteLine("The processor is loading plugin " + j.ActionMessage);
                AssemblyName an = AssemblyName.GetAssemblyName(Path.Combine(pluginLocation, j.ActionMessage + ".dll"));
                Debug.WriteLine("Plugin: " + an.FullName);
                _pluginAssembly = Assembly.Load(an);
                Type[] types = _pluginAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsInterface || type.IsAbstract) { continue; }
                    if (type.FullName == "LocalProcessor.Processor") // have have an interface compatible plugin
                    {
                        _plugin = Activator.CreateInstance(type) as IProcessor;
                        if(_plugin != null)
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
            catch(Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
                _outbound.Enqueue(j);
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
    }
}
