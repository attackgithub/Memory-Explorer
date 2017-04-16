using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class OffsetMap
    {
        public string Md5;
        public HashSet<ulong> OffsetRecords;
    }
    public abstract class ToolBase
    {
        protected Profile _profile = null;
        protected bool _isx64;
        protected DataProviderBase _dataProvider = null;
        protected DataModel _model = null;
        protected MxObjectTypes _objectTypes = null;

        public ToolBase(DataModel model)
        {
            _model = model;
            _profile = model.ActiveProfile;
            _dataProvider = model.DataProvider;
            _isx64 = (_profile.Architecture == "AMD64");
        }
        protected List<LIST_ENTRY> FindAllLists(DataProviderBase dataProvider, LIST_ENTRY source)
        {
            List<LIST_ENTRY> results = new List<LIST_ENTRY>();
            List<ulong> seen = new List<ulong>();
            List<LIST_ENTRY> stack = new List<LIST_ENTRY>();
            AddressBase addressSpace = _model.ActiveAddressSpace;
            stack.Add(source);
            while (stack.Count > 0)
            {
                LIST_ENTRY item = stack[0];
                stack.RemoveAt(0);
                if (!seen.Contains(item.PhysicalAddress))
                {
                    seen.Add(item.PhysicalAddress);
                    results.Add(item);
                    ulong Blink = item.Blink;
                    if (Blink != 0)
                    {
                        ulong refr = addressSpace.vtop(Blink);
                        stack.Add(new LIST_ENTRY(_model, item.Blink));
                    }
                    ulong Flink = item.Flink;
                    if (Flink != 0)
                    {
                        ulong refr = addressSpace.vtop(Flink);
                        stack.Add(new LIST_ENTRY(_model, item.Flink));
                    }
                }
            }
            return results;
        }
        protected bool PersistOffsetMap(OffsetMap source, string fileName)
        {
            if (!fileName.EndsWith(".gz"))
                fileName += ".gz";
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
            return true;
        }
        public OffsetMap RetrieveOffsetMap(FileInfo sourceFile)
        {
            try
            {
                byte[] buffer;
                using (FileStream fs = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    buffer = br.ReadBytes((int)sourceFile.Length);
                }
                byte[] decompressed = Decompress(buffer);
                return JsonConvert.DeserializeObject<OffsetMap>(Encoding.UTF8.GetString(decompressed));
            }
            catch { return null; }
        }
        protected byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressedMs = new MemoryStream(inputData))
            {
                using (var decompressedMs = new MemoryStream())
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressedMs, CompressionMode.Decompress)))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }
        public string GetMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var h = md5.ComputeHash(stream);
                    return GetMd5Hash(h);
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
        //protected byte[] ReadBytes(ulong offset, int count)
        //{
        //    try
        //    {
        //        byte[] buffer = new byte[count];
        //        FileInfo fi = new FileInfo(_imageFile);
        //        FileStream fs = fi.OpenRead();
        //        fs.Seek((long)offset, SeekOrigin.Begin);
        //        fs.Read(buffer, 0, count);
        //        fs.Close();
        //        return buffer;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ArgumentException("Error Reading Data - " + ex.Message);
        //    }
        //}
        //protected int ReadInt32(ulong offset)
        //{
        //    byte[] buffer = ReadBytes(offset, 4);
        //    return BitConverter.ToInt32(buffer, 0);
        //}
        //protected long ReadInt64(ulong offset)
        //{
        //    byte[] buffer = ReadBytes(offset, 8);
        //    return BitConverter.ToInt64(buffer, 0);
        //}
        //protected uint ReadUInt32(ulong offset)
        //{
        //    byte[] buffer = ReadBytes(offset, 4);
        //    return BitConverter.ToUInt32(buffer, 0);
        //}
        //protected ulong ReadUInt64(ulong offset)
        //{
        //    byte[] buffer = ReadBytes(offset, 8);
        //    return BitConverter.ToUInt64(buffer, 0);
        //}
        //protected string ReadString(ulong offset, int length = -1)
        //{
        //    FileInfo fi = new FileInfo(_imageFile);
        //    FileStream fs = fi.OpenRead();
        //    fs.Seek((long)offset, SeekOrigin.Begin);

        //    if (length == -1)
        //    {
        //        string result = "";
        //        int i = fs.ReadByte();
        //        while (i != 0 && i != -1)
        //        {
        //            result += (char)i;
        //            i = fs.ReadByte();
        //        }
        //        fs.Close();
        //        return result;
        //    }
        //    else
        //    {
        //        byte[] buffer = new byte[length];
        //        fs.Read(buffer, 0, length);
        //        fs.Close();
        //        return Encoding.UTF8.GetString(buffer);
        //    }
        //}
        protected List<HandleTableEntry> EnumerateHandles(ulong virtualAddress, uint level)
        {
            return MakeHandleArray(virtualAddress, level);
        }
        protected List<HandleTableEntry> MakeHandleArray(ulong virtualAddress, uint level, int handleAddress = 0)
        {
            List<HandleTableEntry> results = new List<HandleTableEntry>();

            //    Profile profile = _project["profile"] as Profile;
            //    AddressBase kernelAS;
            //    if (_isx64)
            //        kernelAS = _project["kernelAs"] as AddressSpacex64;
            //    else
            //        kernelAS = _project["kernelAs"] as AddressSpacex86Pae;

            //    ulong pageAddress = kernelAS.vtop(virtualAddress);
            byte[] buffer = _dataProvider.ReadMemoryBlock(virtualAddress, 0x1000); //  ReadBytes(pageAddress, 4096);
            if (buffer == null)
                return null;

            if (level == 0)
            {
                // if level is zero then we have an array of handle entries
                int count = _isx64 ? 256 : 512;
                byte[] transfer = _isx64 ? new byte[16] : new byte[8];
                for (int i = 0; i < count; i++)
                {
                    Array.Copy(buffer, (4096 / count) * i, transfer, 0, 4096 / count);
                    HandleTableEntry hte = new HandleTableEntry(_model, transfer, handleAddress);
                    if (hte.IsValid)
                        results.Add(hte);
                    handleAddress++;
                }
            }
            else
            {
                // otherwise we have an array of pointers to more handle index pages                
                int count = _isx64 ? 512 : 1024;
                for (int i = 0; i < count; i++)
                {
                    ulong ptr = _isx64 ? (BitConverter.ToUInt64(buffer, (int)i * 8) & 0xffffffffffff) : BitConverter.ToUInt32(buffer, (int)i * 4);
                    if (ptr == 0)
                        continue;
                    List<HandleTableEntry> partialResults = MakeHandleArray(ptr, level - 1, i * count / 2);
                    foreach (HandleTableEntry h in partialResults)
                        results.Add(h);
                }
            }
            return results;
        }
        protected string GetObjectName(ulong type, ulong vAddr=0, byte cookie=0)
        {
            if (_objectTypes == null)
            {
                string archiveFile = Path.Combine(_model.DataProvider.CacheFolder, "1005.dat");
                FileInfo fi = new FileInfo(archiveFile);
                if (fi.Exists)
                {
                    _objectTypes = new MxObjectTypes(_model);
                }
                foreach (var record in _objectTypes.Records)
                {
                    Debug.WriteLine("Address: 0x" + record.vAddress.ToString("X8") + "\tIndex: " + record.Index + "\tCount: " + record.TotalNumberOfObjects + "\tObject: " + record.Name);
                }
            }
            try
            {
                // if it's windows 10, you'll need to de-obfuscate
                // return ((vaddr >> 8) ^ cookie ^ int(self.m("TypeIndex"))) & 0xFF
                double version = _model.OsVersion;
                if (version == 0)
                {
                    try
                    {
                        string v = GetArchiveItem("1004.dat", 0);
                        if (v != "")
                            version = double.Parse(v);
                    }
                    catch { }
                    
                }
                ulong realValue = type;
                if (version > 9.0)
                    realValue = (ulong)(((int)((vAddr >> 8) ^ type ^ cookie)) & 0xff);
                foreach (ObjectTypeRecord item in _objectTypes.Records)
                {
                    if (item.Index == realValue)
                        return item.Name;
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }

        }
        protected string GetArchiveItem(string archiveFile, int index)
        {
            try
            {
                string file = Path.Combine(_model.DataProvider.CacheFolder, archiveFile);
                FileInfo fi = new FileInfo(file);
                if (fi.Exists)
                {
                    string[] items = File.ReadAllLines(file);
                    return items[index];
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
