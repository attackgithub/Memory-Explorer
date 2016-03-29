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
        public LiveDataProvider(DataModel data) : base(data)
        {
            _helperLib = LoadLibrary(_libraryFilename);

        }
        ~LiveDataProvider()
        {
            FreeLibrary(_helperLib);
        }
        protected override byte[] ReadMemoryPage(ulong address)
        {
            byte[] buffer = GetPage(address);
            return buffer;
        }
        public override byte[] ReadMemory(ulong startAddress, uint pageCount = 1)
        {
            return ReadMemoryPage(startAddress);
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
