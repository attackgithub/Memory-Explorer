using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryExplorer.Model;
using MemoryExplorer.Data;
using System.IO;
using MemoryExplorer.Info;
using MemoryExplorer.Address;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;

namespace MemoryExplorerTests
{
    [TestClass()]
    public sealed class WinXPTest
    {
        private static DataModel _dataModel;
        private static string _cacheLocation;
        private static AddressBase _kernelAddressSpace;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _kernelAddressSpace = null;
            _cacheLocation = @"E:\Forensics\MxProjects\UnitTests\cache";
            DirectoryInfo di = new DirectoryInfo(_cacheLocation);
            foreach (FileInfo fi in di.GetFiles())
            {
                fi.Delete();
            }
            _dataModel = new DataModel(false);
            _dataModel.MemoryImageFilename = @"E:\Forensics\MxProjects\stuxnet\stuxnet.vmem";
            _dataModel.DataProvider = new ImageDataProvider(_dataModel, _cacheLocation);
            string imageMd5 = _dataModel.GetMD5HashFromFile(_dataModel.MemoryImageFilename);
            FileInfo fi2 = new FileInfo(_dataModel.MemoryImageFilename);
            _dataModel.CacheLocation = fi2.Directory.FullName + "\\[" + fi2.Name + "]" + imageMd5;
            DirectoryInfo di2 = new DirectoryInfo(_dataModel.CacheLocation);
            if (!di2.Exists)
                di2.Create();
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
            Assert.IsTrue(test.Name == "30B5FB31AE7E4ACAABA750AA241FF3311.gz");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Architecture"));
            test = _dataModel.InfoDictionary["Architecture"];
            Assert.IsTrue(test.Name == "I386");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Debug Symbols Filename"));
            test = _dataModel.InfoDictionary["Debug Symbols Filename"];
            Assert.IsTrue(test.Name == "ntkrnlpa.pdb");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Debug Symbols (RSDS)"));
            test = _dataModel.InfoDictionary["Debug Symbols (RSDS)"];
            Assert.IsTrue(test.PhysicalAddress == 0x4e0578);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("KiUserSharedData"));
            test = _dataModel.InfoDictionary["KiUserSharedData"];
            Assert.IsTrue(test.Name == "0xFFDF0000");
        }
        [TestMethod()]
        public void TestFindKernelDtb()
        {
            // this actually returns the physical address of the
            // EPROCESS structure for the Idel process
            ulong answer = _dataModel.FindKernelDtbBody();
            Assert.IsTrue(answer == 0x2165300);
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 7);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Directory Table Base"));
            InfoHelper test = _dataModel.InfoDictionary["Directory Table Base"];
            Assert.IsTrue(test.Name == "0x001AA000 (1744896)");
        }
        [TestMethod()]
        public void TestLoadKernelAddressSpace()
        {
            _kernelAddressSpace = new AddressSpacex64(_dataModel.DataProvider, "idle", 0x1aa000, true);
            Assert.IsNotNull(_kernelAddressSpace);
            Assert.IsTrue(_kernelAddressSpace.Dtb == 0x1aa000);
            Assert.IsNotNull(_kernelAddressSpace.MemoryMap);
            Assert.IsTrue(_kernelAddressSpace.MemoryMap.MemoryRecords.Count == 0x9634e);
            Assert.IsTrue(_kernelAddressSpace.MemoryMap.MemoryRecords[4].Flags == 0x901);
            Assert.IsFalse(_kernelAddressSpace.MemoryMap.MemoryRecords[4].IsSoftware);
            Assert.IsTrue(_kernelAddressSpace.MemoryMap.MemoryRecords[4].PhysicalAddress == 0x459000);
            Assert.IsTrue(_kernelAddressSpace.MemoryMap.MemoryRecords[4].VirtualAddress == 0xb0019c009000);
            Assert.IsTrue(_kernelAddressSpace.MemoryMap.MemoryRecords[4].Size == 0x1000);
        }
        [TestMethod()]
        public void TestFindKernelImage()
        {
            _dataModel.FindKernelImageBody(_kernelAddressSpace);
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 10);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Kernel Base Address"));
            InfoHelper test = _dataModel.InfoDictionary["Kernel Base Address"];
            Assert.IsTrue(test.VirtualAddress == 0xf801ee018000);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Build String"));
            test = _dataModel.InfoDictionary["Build String"];
            Assert.IsTrue(test.Name == "9600.winblue_gdr.140305-1710");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Build String Ex"));
            test = _dataModel.InfoDictionary["Build String Ex"];
            Assert.IsTrue(test.Name == "9600.17041.amd64fre.winblue_gdr.140305-1710");
        }
        [TestMethod()]
        public void TestFindUserSharedData()
        {
            _dataModel.FindUserSharedDataBody();
            Assert.IsTrue(_dataModel.InfoDictionary.Count == 14);
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Version"));
            InfoHelper test = _dataModel.InfoDictionary["Version"];
            Assert.IsTrue(test.Name == "6.3 (Windows 8.1 or 2012 R2)");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Physical Page Count"));
            test = _dataModel.InfoDictionary["Physical Page Count"];
            Assert.IsTrue(test.Name == "262013");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("System Root"));
            test = _dataModel.InfoDictionary["System Root"];
            Assert.IsTrue(test.Name == "C:\\Windows");
            Assert.IsTrue(_dataModel.InfoDictionary.ContainsKey("Active Processor Count"));
            test = _dataModel.InfoDictionary["Active Processor Count"];
            Assert.IsTrue(test.Name == "1");
        }
        [TestMethod()]
        public void TestEnumerateObjectTypes()
        {
            Profile_Deprecated prof = _dataModel.GetProfile_Deprecated;
            prof.KernelAddressSpace = _kernelAddressSpace;
            InfoHelper test = _dataModel.InfoDictionary["Kernel Base Address"];
            prof.KernelBaseAddress = test.VirtualAddress;
            _dataModel.DataProvider.ActiveAddressSpace = _kernelAddressSpace;
            ObjectTypes objectTypes = new ObjectTypes(_dataModel.DataProvider, prof);
            Assert.IsNotNull(objectTypes);
        }
    }
}
