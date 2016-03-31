using MemoryExplorer.Memory;
using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Data
{
    public class LiveDataProvider : DataProviderBase
    {
        string _libraryFilename = @"C:\Users\mark\OneDrive\Code\MvvmPlaytime\Resources\DriverLib.dll";
        IntPtr _helperLib;
        private List<MemoryRange> _memoryRangeList = new List<MemoryRange>();
        private ulong _maximumPhysicalAddress = 0;

        public LiveDataProvider(DataModel data) : base(data)
        {
            _libraryFilename = data.HelperLibrary;
            _helperLib = LoadLibrary(_libraryFilename);

        }
        ~LiveDataProvider()
        {
            FreeLibrary(_helperLib);
        }
        protected override byte[] ReadMemoryPage(ulong address)
        {
            foreach (var item in _memoryRangeList)
            {
                if(address >= item.StartAddress && address <= (item.StartAddress + item.Length - 0x1000))
                {
                    byte[] buffer = GetPage(address);
                    return buffer;
                }
            }
            return null;
        }
        public override byte[] ReadMemory(ulong startAddress, uint pageCount = 1)
        {
            try
            {
                byte[] buffer = new byte[pageCount * 0x1000];
                for (uint i = 0; i < pageCount; i++)
                {
                    byte[] tempBuffer = ReadMemoryPage(startAddress);
                    if (tempBuffer != null)
                        Array.Copy(tempBuffer, 0, buffer, (i * 0x1000), 0x1000);
                    else
                        throw new ArgumentException("Unable to read requested memory");
                }
                return buffer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        private byte[] GetPage(ulong address)
        {
            byte[] buffer = new byte[4096];
            uint error = GetLastError();
            IntPtr procAddress = GetProcAddress(_helperLib, "ReadPage");
            ReadPage rp = (ReadPage)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(ReadPage));
            int result = rp(address, buffer);
            if (result == 0)
                return buffer;
            return null;
        }
        public override Dictionary<string, object> GetInformation()
        {
            Dictionary<string, object> _information = new Dictionary<string, object>();
            uint error = GetLastError();
            byte[] buffer = new byte[4096];
            IntPtr procAddress = GetProcAddress(_helperLib, "GetInfo");
            GetInfo info = (GetInfo)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(GetInfo));
            int result2 = info(buffer);
            if (result2 == 0)
            {
                ulong itemValue = BitConverter.ToUInt64(buffer, 0);
                _information.Add("dtb", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 8);
                _information.Add("buildNumber", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 16);
                _information.Add("kernelBase", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 24);
                _information.Add("kdbg", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 288);
                _information.Add("pfnDatabase", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 296);
                _information.Add("psLoadedModuleList", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 304);
                _information.Add("ntBuildNumberAddress", itemValue);
                itemValue = BitConverter.ToUInt64(buffer, 2352);
                _information.Add("runCount", itemValue);
                int count = (int)itemValue;
                for (int i = 0; i < count; i++)
                {
                    MemoryRange range = new MemoryRange();
                    range.StartAddress = BitConverter.ToUInt64(buffer, 2360 + (i * 16));
                    range.Length = BitConverter.ToUInt64(buffer, 2368 + (i * 16));
                    range.PageCount = (uint)(range.Length / 4096);
                    _memoryRangeList.Add(range);
                    if (range.StartAddress + range.Length > _maximumPhysicalAddress)
                        _maximumPhysicalAddress = range.StartAddress + range.Length;
                }
                _information.Add("maximumPhysicalAddress", _maximumPhysicalAddress);
                ImageLength = _maximumPhysicalAddress;
                _information.Add("memoryRanges", _memoryRangeList);
            }
            return _information;
        }
        #region NativeMethods
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int GetInfo([MarshalAs(UnmanagedType.LPArray)]byte[] buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ReadPage(ulong address, [MarshalAs(UnmanagedType.LPArray)]byte[] buffer);


        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string dllName);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32.dll")]
        internal static extern uint GetLastError();
        [DllImport("kernel32.dll")]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool FreeLibrary(IntPtr handle);

        #endregion
    }
}
