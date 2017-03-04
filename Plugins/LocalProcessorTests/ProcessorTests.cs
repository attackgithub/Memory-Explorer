using LocalProcessor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalProcessorTests
{
    [TestClass]
    public class ProcessorTests
    {
        [TestMethod]
        [TestCategory("plugin")]
        public void TestProcessorPluginName()
        {
            Processor p = new Processor();
            Assert.IsTrue(p.Name == "Local Processor Plugin");
        }
    }
}
