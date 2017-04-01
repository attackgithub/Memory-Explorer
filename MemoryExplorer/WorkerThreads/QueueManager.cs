using Library.LogFileHelper;
using MemoryExplorer.Model;
using MemoryExplorer.Worker;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace MemoryExplorer.WorkerThreads
{
    public class QueueManagerThread
    {
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private Queue<Job> _ingesterInbound = null;
        private Queue<Job> _ingesterOutbound = null;
        private Queue<Job> _processorInbound = null;
        private Queue<Job> _processorOutbound = null;

        public QueueManagerThread(DataModel model)
        {
            _backgroundWorker.DoWork += new DoWorkEventHandler(QueueManagerThread_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(QueueManagerThread_RunWorkerCompleted);
            _backgroundWorker.WorkerSupportsCancellation = true;

            // Start the asynchronous operation.
            _backgroundWorker.RunWorkerAsync(model);

        }
        public void Stop()
        {
            Debug.WriteLine("The queue manager is closing");

            _backgroundWorker.CancelAsync();
        }
        private void QueueManagerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            DataModel model = (DataModel)e.Argument;
            _ingesterOutbound = model.IngesterOut; 
            _ingesterInbound = model.IngesterIn;
            _processorInbound = model.ProcessorIn;
            _processorOutbound = model.ProcessorOut;


            while (!worker.CancellationPending)
            {
                if (_ingesterInbound.Count > 0)
                {
                    Job j = _ingesterInbound.Dequeue();
                    switch (j.Status)
                    {
                        case JobStatus.Failed:
                            model.WriteToLogfile("Ingester Error from " + j.Action);
                            model.WriteToLogfile("\t" + j.ErrorMessage);
                            break;
                        default:
                            break;
                    }
                    switch (j.Action)
                    {

                        default:
                            break;
                    }
                }
                else if (_processorInbound.Count > 0)
                {
                    Job j = _processorInbound.Dequeue();
                    switch(j.Status)
                    {
                        case JobStatus.Failed:
                            model.WriteToLogfile("Processor Error from " + j.Action);
                            model.WriteToLogfile("\t" + j.ErrorMessage);
                            string messageBoxText = "There was a problem loading the data provider.\n" + j.ErrorMessage;
                            System.Windows.MessageBox.Show(messageBoxText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        default:
                            break;
                    }
                    switch (j.Action)
                    {
                        case JobAction.SetDataProvider:
                            if(j.Status == JobStatus.Complete)
                            {
                                Job j1 = new Job();
                                j1.Action = JobAction.GetProfileIdentification;
                                _processorOutbound.Enqueue(j1);
                            }
                            break;
                        case JobAction.GetProfileIdentification:
                            if (j.Status == JobStatus.Complete)
                            {
                                // there is a possibility that more than one guidage was identified
                                // so I need to work out how to deal with that
                                if(j.ActionMessage.Count > 1)
                                {
                                    string messageBoxText = "Memory Explorer has identified more than one GUIDAGE in the image.\nGoing to proceed with the first one.";
                                    System.Windows.MessageBox.Show(messageBoxText, "Strangeness", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                if (j.ActionMessage.Count > 0)
                                { 
                                    Job j1 = new Job();
                                    j1.Action = JobAction.LoadProfile;
                                    j1.ActionMessage.Add(j.ActionMessage[0]);
                                    _processorOutbound.Enqueue(j1);
                                }
                                else
                                {
                                    string messageBoxText = "Processing Halted because Memory Explorer couldn't find a Profile from RSDS";
                                    System.Windows.MessageBox.Show(messageBoxText, "Can't Proceed", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            break;
                        case JobAction.LoadProfile:
                            if (j.Status == JobStatus.Complete)
                            {
                                Job j1 = new Job();
                                j1.Action = JobAction.FindKernelDtb;
                                _processorOutbound.Enqueue(j1);
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    //Debug.WriteLine("The queue manager is waiting");
                    Thread.Sleep(5);
                }
            }
            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            //e.Result = ComputeFibonacci((int)e.Argument, worker, e);
        }
        private void QueueManagerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show(e.Error.Message);
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
