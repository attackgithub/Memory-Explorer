using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public class StructureHelper
    {
        public string Md5;
        public List<ProfileEntry> StructureEntries;
    }
    public partial class DataModel : INotifyPropertyChanged
    {
        private void ClearInfoTree()
        {
            _profileEntries.Clear();
            NotifyPropertyChange("ProfileTreeItems");
        }
        private void PopulateInfoTree(string structureName)
        {
            int structureSize = 0;
            try
            {
                structureSize = (int)_profile_deprecated.GetStructureSize(structureName);
                if (structureSize == -1)
                    return;
            }
            catch (Exception)
            {
                return;
            }
            
            _profileEntries.Clear();
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\structure_" + structureName + ".gz");
            if (cachedFile.Exists)
            {
                StructureHelper cachedMap = RetrieveStructureTree(cachedFile);
                if (cachedMap != null)
                {
                    foreach (ProfileEntry item in cachedMap.StructureEntries)
                        _profileEntries.Add(item);
                }
                NotifyPropertyChange("ProfileTreeItems");
                return;
            }

            string realName = structureName.TrimStart(new char[] { '_' });
            ProfileEntry root = new ProfileEntry();
            root.Name = realName + " [" + structureSize + "]";
            root.Parent = null;
            root.IsExpanded = true;
            root.Offset = 0;
            root.Length = (uint)structureSize;
            _profileEntries.Add(root);
            PopulateNode(structureName, root, 0);
            
            NotifyPropertyChange("ProfileTreeItems");
            // persist the tree records to save time next time
            StructureHelper sh = new StructureHelper();
            sh.StructureEntries = new List<ProfileEntry>();
            foreach(ProfileEntry e in _profileEntries)
                sh.StructureEntries.Add(e);
            PersistStructureTree(sh, _dataProvider.CacheFolder + "\\structure_" + structureName);
        }
        private void PopulateNode(string structureName, ProfileEntry parent, ulong offset, bool expanded = false)
        {
            List<Structure> results = _profile_deprecated.GetEntries(structureName);
            foreach (Structure s in results)
            {
                ProfileEntry next = new ProfileEntry();
                next.Name = s.Name + " [" + (s.Offset + offset).ToString() + " , " + s.Size + " , " + s.EntryType + "]";
                next.Parent = parent;
                next.IsExpanded = expanded;
                next.Offset = (uint)(s.Offset + offset);
                next.Length = (uint)s.Size;
                _profileEntries.Add(next);
                int structureSize = 0;
                try
                {
                    structureSize = (int)_profile_deprecated.GetStructureSize(s.EntryType);
                    if (structureSize != -1)
                        PopulateNode(s.EntryType, next, s.Offset + offset);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
        protected bool PersistStructureTree(StructureHelper source, string fileName)
        {
            uint marker = 0;
            foreach (ProfileEntry item in source.StructureEntries)
                item.RecordNumber = marker++;
            foreach (ProfileEntry item in source.StructureEntries)
            {
                ProfileEntry parent = item.Parent;
                if (parent != null)
                    item.ParentRecordNumber = parent.RecordNumber;
            }

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
        public StructureHelper RetrieveStructureTree(FileInfo sourceFile)
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
                StructureHelper sh = JsonConvert.DeserializeObject<StructureHelper>(Encoding.UTF8.GetString(decompressed));
                foreach (ProfileEntry item in sh.StructureEntries)
                {
                    if (item.RecordNumber == 0)
                        continue;
                    foreach(ProfileEntry parentItem in sh.StructureEntries)
                    {
                        if (parentItem.RecordNumber == item.ParentRecordNumber)
                        {
                            item.Parent = parentItem;
                            break;
                        }
                    }
                }
                return sh;
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
    }
}
