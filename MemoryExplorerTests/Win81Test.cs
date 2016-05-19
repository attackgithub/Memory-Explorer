using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryExplorer.Model;
using MemoryExplorer.Data;
using System.IO;
using MemoryExplorer.Info;

namespace MemoryExplorerTests
{
    [TestClass()]
    public sealed class Win81Test
    {
        private static DataModel _dataModel;
        private static string _cacheLocation;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _cacheLocation = @"E:\Forensics\MxProjects\UnitTests\cache";
            DirectoryInfo di = new DirectoryInfo(_cacheLocation);
            foreach (FileInfo fi in di.GetFiles())
            {
                fi.Delete();
            }
            _dataModel = new DataModel(false);
            _dataModel.MemoryImageFilename = @"E:\Forensics\MxProjects\win8\win8.1.vmem";
            _dataModel.DataProvider = new ImageDataProvider(_dataModel, _cacheLocation);
        }
        [ClassCleanup()]
        public static void ClassCleanup()
        {
            DirectoryInfo di = new DirectoryInfo(_cacheLocation);
            foreach (FileInfo fi in di.GetFiles())
                fi.Delete();
        }
        [TestMethod()]
        public void TestGetInformation()
        {
            _dataModel.GetInformationBody();
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 1);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Maximum Physical Address"));
        }
        [TestMethod()]
        public void TestFindProfileGuid()
        {
            _dataModel.FindProfileGuidBody();
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 6);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("ProfileName"));
            InfoHelper test = _dataModel.InfoDictionary["ProfileName"];
            Assert.IsTrue(test.Name == "D67FECD43A49492C87EC845789255D641.gz");            
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Architecture"));
            test = _dataModel.InfoDictionary["Architecture"];
            Assert.IsTrue(test.Name == "AMD64");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Debug Symbols Filename"));
            test = _dataModel.InfoDictionary["Debug Symbols Filename"];
            Assert.IsTrue(test.Name == "ntkrnlmp.pdb");            
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Debug Symbols (RSDS)"));
            test = _dataModel.InfoDictionary["Debug Symbols (RSDS)"];
            Assert.IsTrue(test.PhysicalAddress == 0x1e32e40);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("KiUserSharedData"));
            test = _dataModel.InfoDictionary["KiUserSharedData"];
            Assert.IsTrue(test.Name == "0xFFFFF78000000000");
        }
        [TestMethod()]
        public void TestFindKernelDtb()
        {
            ulong answer = _dataModel.FindKernelDtbBody();
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 7);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Directory Table Base"));
            InfoHelper test = _dataModel.InfoDictionary["Directory Table Base"];
            Assert.IsTrue(test.Name == "0x001AA000 (1744896)");
        }
    }
}
