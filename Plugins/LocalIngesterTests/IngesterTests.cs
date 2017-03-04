using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LocalIngester;

namespace LocalIngesterTests
{
    [TestClass]
    public class IngesterTests
    {
        [TestMethod]
        [TestCategory("plugin")]
        public void TestIngesterPluginName()
        {
            Ingester i = new Ingester();
            Assert.IsTrue(i.Name == "Local Ingester Plugin");
        }
    }
}
