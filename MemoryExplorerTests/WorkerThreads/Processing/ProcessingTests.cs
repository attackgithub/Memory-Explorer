using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryExplorer.Worker;
using MemoryExplorer.WorkerThreads;
using MemoryExplorer.Model;
using MemoryExplorer.Data;

namespace MemoryExplorerTests.WorkerThreads.Processing
{
    [TestClass]
    public class ProcessingTests
    {
        public ProcessingThread DataProvider { get; private set; }

        [TestMethod]
        [TestCategory("processing")]
        public void TestingGetProfileIdentification()
        {
            Job j = new Job();
            DataModel model = new DataModel(true);
            DataProviderBase provider = new ImageDataProvider(model, @"c:\temp");
            ProcessingThread th = new ProcessingThread(model);
            th.GetProfileIdentifier(ref j);

        }
    }
}
