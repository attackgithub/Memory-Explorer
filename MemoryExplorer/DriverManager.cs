using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;

namespace MemoryExplorer
{
    public class DriverManager
    {
        private string _driverFilename;
        private string _libraryFilename;
        private bool _driverLoaded = false;
        private IntPtr _helperLib = IntPtr.Zero;

        public string LibraryFilename { get { return _libraryFilename; } }

        public DriverManager()
        {
            if(!IsAdmin())
                throw new ArgumentException("Can't Load Driver - You need to be ADMIN");
            _driverFilename = ExtractResources("winpmem_x64.sys");
            if (_driverFilename == null)
                throw new ArgumentException("Problem Extracting the Device Driver");
            _libraryFilename = ExtractResources("DriverLib.dll");
            if (_libraryFilename == null)
                throw new ArgumentException("Problem Extracting the Helper Library");
            _helperLib = LoadLibrary(_libraryFilename);
            if(_helperLib == IntPtr.Zero)
                throw new ArgumentException("Problem Loading Helper Library");
        }
        ~DriverManager()
        {
            try
            {
                if (_driverLoaded && IsAdmin())
                    UnloadDriver();
                if (_helperLib != IntPtr.Zero)
                    FreeLibrary(_helperLib);
            }
            catch { }
        }
        private string ExtractResources(string ResourceName)
        {
            string resourceFilename = null;

            try
            {
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(ResourceName));
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    resourceFilename = Path.GetTempFileName();
                    File.WriteAllBytes(resourceFilename, assemblyData);
                    // be a good citizen and clean up any old versions of the file
                    FileInfo fi = new FileInfo(resourceFilename);
                    FileInfo[] files = fi.Directory.GetFiles("*.tmp");
                    var md5 = MD5.Create();
                    byte[] originalHash = md5.ComputeHash(assemblyData);
                    foreach (var item in files)
                    {
                        //don't delete the last extracted copy
                        if (item.FullName == resourceFilename)
                        {
                            continue;
                        }
                        if (item.Length == assemblyData.Length)
                        {
                            var stream2 = File.OpenRead(item.FullName);
                            byte[] myHash = md5.ComputeHash(stream2);
                            stream2.Close();
                            if (originalHash.SequenceEqual(myHash))
                                item.Delete();

                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return resourceFilename;
        }
        public bool LoadDriver()
        {
            if (_driverLoaded)
                return true;
            uint error = GetLastError();
            IntPtr procAddress = GetProcAddress(_helperLib, "Version");
            GetVersion ver = (GetVersion)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(GetVersion));
            int realVersion = ver();
            bool result;
            // register driver
            procAddress = GetProcAddress(_helperLib, "RegisterDriver");
            RegisterDriver reg = (RegisterDriver)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(RegisterDriver));
            result = reg("pmem", _driverFilename);
            if (result == false)
            {
                _driverLoaded = false;
                return result;
            }
            // start driver
            procAddress = GetProcAddress(_helperLib, "StartDriver");
            StartDriver start = (StartDriver)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(StartDriver));
            result = start("pmem");
            error = GetLastError();
            _driverLoaded = true;
            return result;
        }
        public bool UnloadDriver()
        {
            if (!_driverLoaded)
                return true;
            IntPtr procAddress = GetProcAddress(_helperLib, "Version");
            GetVersion ver = (GetVersion)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(GetVersion));
            int realVersion = ver();
            bool result;
            // register driver
            procAddress = GetProcAddress(_helperLib, "StopDriver");
            StopDriver stop = (StopDriver)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(StopDriver));
            result = stop("pmem");
            if (result == false)
            {
                return result;
            }
            // start driver
            procAddress = GetProcAddress(_helperLib, "UnregisterDriver");
            UnregisterDriver unreg = (UnregisterDriver)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(UnregisterDriver));
            result = unreg("pmem");
            _driverLoaded = false;
            return result;
        }
        private bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principle = new WindowsPrincipal(identity);
            return principle.IsInRole(WindowsBuiltInRole.Administrator);
        }
        #region NativeMethods
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int GetVersion();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool RegisterDriver([MarshalAs(UnmanagedType.LPWStr)]string driverName, [MarshalAs(UnmanagedType.LPWStr)]string path);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool StartDriver([MarshalAs(UnmanagedType.LPWStr)]string driverName);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool StopDriver([MarshalAs(UnmanagedType.LPWStr)]string driverName);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool UnregisterDriver([MarshalAs(UnmanagedType.LPWStr)]string driverName);

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
