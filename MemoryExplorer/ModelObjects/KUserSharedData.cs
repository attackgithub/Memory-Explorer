using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class KUserSharedData
    {
        private dynamic _sd;
        private Profile _profile;
        private ulong _physicalAddress;

        public KUserSharedData(ulong physicalAddress, Profile profile)
        {
            _profile = profile;
            _physicalAddress = physicalAddress;
            _sd = profile.GetStructure("_KUSER_SHARED_DATA", physicalAddress);
        }
        public dynamic dynamicObject
        {
            get { return _sd; }
        }
        public string Version
        {
            get
            {
                try
                {
                    var major = _sd.NtMajorVersion;
                    var minor = _sd.NtMinorVersion;
                    return (major.ToString() + "." + minor.ToString());
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract version elements from current KUSER_SHARED_DATA structure.");
                }
            }
        }
        public uint NumberOfPhysicalPages
        {
            get
            {
                try
                {
                    return (uint)_sd.NumberOfPhysicalPages;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract NumberOfPhysicalPages from current KUSER_SHARED_DATA structure.");
                }
            }
        }
        public string NtSystemRoot
        {
            get
            {
                try
                {
                    string result = "";
                    var nsr = _sd.NtSystemRoot;
                    for (int i = 0; i < nsr.Length; i++)
                    {
                        if (nsr[i] == 0)
                            break;
                        result += Convert.ToChar(nsr[i]);
                    }
                    return result;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract NtSystemRoot from current KUSER_SHARED_DATA structure.");
                }
            }
        }
        public UInt64 SystemTime
        {
            get
            {
                try
                {
                    var th = _sd.SystemTime.High1Time;
                    var tl = _sd.SystemTime.LowPart;
                    UInt64 tt = (UInt64)((th * 0x100000000) + tl);
                    //DateTime dt = (new DateTime(tt)).AddYears(1600);
                    return tt;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Couldn't extract SystemTime from current KUSER_SHARED_DATA structure.");
                }
            }
        }
    }
}
