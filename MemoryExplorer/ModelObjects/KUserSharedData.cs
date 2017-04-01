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
        private Profile_Deprecated _profile;
        private DataProviderBase _dataProvider;
        private byte[] _buffer = null;

        public KUserSharedData(DataProviderBase dataProvider, Profile_Deprecated profile, ulong physicalAddress)
        {
            _dataProvider = dataProvider;
            _profile = profile;
            int structureSize = (int)_profile.GetStructureSize("_KUSER_SHARED_DATA");
            if (structureSize == -1)
                throw new ArgumentException("Error - Profile didn't contain a definition for _KUSER_SHARED_DATA");
            // need to ix this if there is a likelyhood that the structure is bigger than a single page
            // for now just be lazy and assume it';; fit into a single memory page.
            if(structureSize > 4096)
                throw new ArgumentException("Error - _KUSER_SHARED_DATA is larger than a single memory page");

            _buffer = _dataProvider.ReadMemory(physicalAddress, 1);
        }
        public object Get(string member)
        {
            try
            {
                int offset = (int)_profile.GetOffset("_KUSER_SHARED_DATA", member);
                uint objectSize = _profile.GetSize("_KUSER_SHARED_DATA", member);
                bool isArray = _profile.IsArray("_KUSER_SHARED_DATA", member);
                switch (objectSize)
                {
                    case 1:
                        return _buffer[offset];
                    case 2:
                        return BitConverter.ToUInt16(_buffer, offset);
                    case 4:
                        return BitConverter.ToUInt32(_buffer, offset);
                    case 8:
                        return BitConverter.ToUInt64(_buffer, offset);
                    default:
                        if (isArray)
                        {
                            byte[] array = new byte[objectSize];
                            Array.Copy(_buffer, offset, array, 0, objectSize);
                            return array;
                        }
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public string GetString(string member)
        {
            try
            {
                var t = _profile.GetType("_KUSER_SHARED_DATA", member);
                int offset = (int)_profile.GetOffset("_KUSER_SHARED_DATA", member);
                if (t == "UnicodeString")
                {
                    ulong strlen = _profile.GetUnicodeStringLength("_KUSER_SHARED_DATA", member);
                    byte[] buffer = new byte[strlen * 2];
                    Array.Copy(_buffer, offset, buffer, 0, (int)(strlen * 2));
                    return Encoding.Unicode.GetString(buffer).Trim(new char[] { '\x0' });
                }
                if (t == "Array")
                {
                    byte[] array = (byte[])Get(member);
                    return Encoding.Unicode.GetString(array).Trim(new char[] { '\x0' });
                }
                return null;

            }
            catch
            {
                return null;
            }
        }
        public string Version
        {
            get
            {
                var test = Get("NtMajorVersion");
                if (test == null)
                    return null;
                string version = test.ToString() + ".";
                test = Get("NtMinorVersion");
                if (test == null)
                    return null;
                version += test.ToString();
                return version;
            }
        }
    }
}
