using MemoryExplorer.Data;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer
{
    public class MemberInfo
    {
        public string Name;
        public ulong Offset;
        public uint Size;
        public bool IsArray;
        public long Count;
    }
    public class Structure
    {
        public string Name;
        public string StructureName;
        public ulong Offset;
        public string EntryType;
        public string PointerType;
        public uint StartBit;
        public uint EndBit;
        public string BitType;
        public string ArrayType;
        public ulong ArrayCount;
        public ulong Size;

        public Structure(string structure)
        {
            StructureName = structure;
        }
    }
    public abstract class StructureBase
    {
        protected Profile _profile;
        protected string _imageFile;
        protected ulong _physicalAddress;
        protected bool _is64;
        protected byte[] _buffer = null;
        protected List<Structure> _structure;
        protected long _structureSize = -1;
        protected DataProviderBase _dataProvider;
        //protected ObjectHeader _header = null;

        protected Structure GetStructureMember(string member)
        {
            foreach (Structure s in _structure)
                if (s.Name == member)
                    return s;
            return null;
        }
        protected byte[] ReadData(ulong offset, int count)
        {
            byte[] buffer = new byte[count];
            FileInfo fi = new FileInfo(_imageFile);
            FileStream fs = fi.OpenRead();
            fs.Seek((long)offset, SeekOrigin.Begin);
            fs.Read(buffer, 0, (int)count);
            fs.Close();
            return buffer;
        }
        public string GetMd5(string filename)
        {
            try
            {
                if (filename != _imageFile)
                    throw new ArgumentException("arrrg");
                //return _profile.Project["fileHash"].ToString();
                return null;
            }
            catch
            {
                using (var md5Array = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var h = md5Array.ComputeHash(stream);
                        return GetMd5Hash(h);
                    }
                }
            }
        }
        string GetMd5Hash(byte[] data)
        {
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public long Size { get { return _structureSize; } }
    }
}
