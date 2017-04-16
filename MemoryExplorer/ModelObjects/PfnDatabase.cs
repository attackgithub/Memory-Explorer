using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
    public class PfnDatabaseMap
    {
        public List<PfnRecord> PfnDatabaseRecords;
    }
    public class PfnRecord
    {
        public u1 U1;
        public u2 U2;
        public long Lock;
        public ulong PteAddress;
        public ulong PteLong;
        public ulong VolatilePteAddress;
        public u3 U3;
        public ushort NodeBlinkLow;
        public byte VaType;
        public byte NodeFlinkLow;
        public byte ViewCount;
        public ulong OriginalPte;
        public u4 U4;
        public ushort E1;
        public uint E2;
        public ulong PhysicalAddress;
        public ulong PtePhysicalLocation;
    }
    public class PfnDatabase : StructureBase
    {
        private List<PfnRecord> _pfnDatabaseList = new List<PfnRecord>();

        public PfnDatabase(DataModel model, ulong virtualAddress) : base(model, virtualAddress)
        {
            _is64 = (_profile.Architecture == "AMD64");
            // there's no point if the system is live
            if (_dataProvider.IsLive)
                return;
            // first let's see if it already exists
            FileInfo cachedFile = new FileInfo(_dataProvider.CacheFolder + "\\pfn_database_map.gz");
            if (cachedFile.Exists && !_dataProvider.IsLive)
            {
                PfnDatabaseMap dbm = RetrievePfnMap(cachedFile);
                if (dbm != null)
                {
                    _pfnDatabaseList = dbm.PfnDatabaseRecords;
                    return;
                }
            }
            int pageCount = (int)(_dataProvider.ImageLength / 0x1000);
            int blockTracker = 25600; // this is how many records are on 300 pages
            byte[] blockBuffer = null;
            for (int i = 0; i < pageCount; i++)
            {
                ulong startAddress = virtualAddress + (uint)(i * 0x30); // assuming pfn records are always 48 bytes long!
                if (blockTracker == 25600)
                {
                    blockTracker = 0;
                    blockBuffer = _dataProvider.ReadMemoryBlock(startAddress, 300 * 0x1000);
                    if (blockBuffer == null)
                        break;
                }
                MMPFN entry = new MMPFN(blockBuffer, blockTracker * 48);
                PfnRecord record = entry.PfnRecord;
                ulong containingPage = record.U4.PteFrame;
                record.PtePhysicalLocation = (containingPage << 12) | record.PteAddress & 0xfff;
                record.PhysicalAddress = (ulong)(i * 0x1000);
                blockTracker++;
                if (record.PteAddress == 0)
                    continue;
                _pfnDatabaseList.Add(record);
            }
            PfnDatabaseMap map = new PfnDatabaseMap();
            map.PfnDatabaseRecords = _pfnDatabaseList;
            if (!_dataProvider.IsLive)
                PersistPfnMap(map, _dataProvider.CacheFolder + "\\pfn_database_map.gz");
        }
        private byte[] NextBlock(ulong startAddress, uint pageCount)
        {
            return _dataProvider.ReadMemoryBlock(startAddress, pageCount * 0x1000);
        }
        public List<PfnRecord> PfnDatabaseList { get { return _pfnDatabaseList; } }

        public void PersistPfnMap(PfnDatabaseMap source, string fileName)
        {
            byte[] bytesToCompress = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
            using (FileStream fileToCompress = File.Create(fileName))
            using (GZipStream compressionStream = new GZipStream(fileToCompress, CompressionMode.Compress))
            {
                compressionStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }
        public PfnDatabaseMap RetrievePfnMap(FileInfo sourceFile)
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
                return JsonConvert.DeserializeObject<PfnDatabaseMap>(Encoding.UTF8.GetString(decompressed));
            }
            catch { return null; }
        }
    }
    public class MMPFN
    {
        [StructLayout(LayoutKind.Explicit, Size =48, Pack =1)]
        public struct _MMPFN
        {
            [FieldOffset(0)]
            public ulong u1;
            [FieldOffset(8)]
            public ulong u2;
            [FieldOffset(16)]
            public ulong PteAddress;
            [FieldOffset(16)]
            public ulong PteLong;
            [FieldOffset(16)]
            public long Lock;
            [FieldOffset(16)]
            public ulong VolatilePteAddress;
            [FieldOffset(24)]
            public uint u3;
            [FieldOffset(24)]
            public uint e2;
            [FieldOffset(26)]
            public ushort e1;
            [FieldOffset(28)]
            public ushort NodeBlinkLow;
            [FieldOffset(30)]
            public byte VaType;
            [FieldOffset(31)]
            public byte NodeFlinkLow;
            [FieldOffset(31)]
            public byte ViewCount;
            [FieldOffset(32)]
            public ulong OriginalPte;
            [FieldOffset(32)]
            public long AweReferenceCount;
            [FieldOffset(40)]
            public ulong u4;
        }
        private PfnRecord _pfnRecord;

        public PfnRecord PfnRecord { get { return _pfnRecord; } }

        public MMPFN(byte[] buffer, int offset)
        {
            GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _MMPFN pfnRecord = (_MMPFN)Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset), typeof(_MMPFN));
            _pfnRecord = new PfnRecord();
            _pfnRecord.Lock = pfnRecord.Lock;
            _pfnRecord.PteAddress = pfnRecord.PteAddress & 0xffffffffffff;
            _pfnRecord.PteLong = pfnRecord.PteLong;
            _pfnRecord.VolatilePteAddress = pfnRecord.VolatilePteAddress;
            _pfnRecord.NodeBlinkLow = pfnRecord.NodeBlinkLow;
            _pfnRecord.VaType = pfnRecord.VaType;
            _pfnRecord.NodeFlinkLow = pfnRecord.NodeFlinkLow;
            _pfnRecord.ViewCount = pfnRecord.ViewCount;

            _pfnRecord.U1 = new u1(pfnRecord.u1);
            _pfnRecord.U2 = new u2(pfnRecord.u2);
            _pfnRecord.U3 = new u3(pfnRecord.u3);
            _pfnRecord.OriginalPte = pfnRecord.OriginalPte;
            _pfnRecord.U4 = new u4(pfnRecord.u4);
            _pfnRecord.E1 = pfnRecord.e1;
            _pfnRecord.E2 = pfnRecord.e2;
            pinnedPacket.Free();
        }
    }
    public class u1
    {
        public ulong Event;
        public ulong Flink;
        public ulong KernelStackOwner;
        public ulong Next;
        public ulong NextStackPfn;
        public ulong NodeFlinkHigh;
        public ulong VolatileNext;
        public ulong WsIndex;

        public u1(ulong entry)
        {
            Event = entry;
            Flink = (ulong)(entry & 0xfffffffff);
            KernelStackOwner = entry;
            Next = entry;
            NextStackPfn = entry;
            NodeFlinkHigh = (ulong)(entry & 0xfffffff000000000) >> 36;
            VolatileNext = entry;
            WsIndex = entry;
        }
    }
    public class u2
    {
        public ulong Blink;
        public ulong ImageProtoPte;
        public ulong NodeBlinkHigh;
        public ulong ShareCount;
        public ulong SpareBlink;
        public ulong TbFlushStamp;

        public u2(ulong entry)
        {
            Blink = (ulong)(entry & 0xfffffffff);
            ImageProtoPte = entry;
            NodeBlinkHigh = (ulong)(entry & 0xfffff000000000) >> 36;
            ShareCount = entry;
            SpareBlink = (ulong)(entry & 0xf000000000000000) >> 60;
            TbFlushStamp = (ulong)(entry & 0xf00000000000000) >> 56;
        }
    }
    public class u3
    {
        public ushort ReferenceCount;
        public MMPFNENTRY E1;
        public e2 E2;

        public u3(uint entry)
        {
            ReferenceCount = (ushort)(entry & 0xffff);
            E1 = new MMPFNENTRY((ushort)((entry & 0xffff0000) >> 16));
            E2 = new e2(entry);
        }
    }
    public class u4
    {
        public ulong EntireField;
        public ulong Channel;
        public ulong PageColour;
        public ulong PageIdentity;
        public bool PfnExists;
        public bool PrototypePte;
        public ulong PteFrame;

        public u4(ulong entry)
        {
            EntireField = entry;
            Channel = (ulong)(entry & 0x3000000000) >> 36;
            PageColour = (ulong)(entry & 0xfc00000000000000) >> 58;
            PageIdentity = (ulong)(entry & 0xc0000000000000) >> 54;
            PfnExists = ((entry & 0x20000000000000) > 0);
            PrototypePte = ((entry & 0x200000000000000) > 0);
            PteFrame = (ulong)(entry & 0xfffffffff);
        }
    }
    public class e2
    {
        public ushort ReferenceCount;
        public ushort ShortFlags;
        public ushort VolatileShortFlags;
        public ushort VolatileReferenceCount;

        public e2(uint entry)
        {
            ShortFlags = (ushort)((entry & 0xffff0000) >> 16);
            ReferenceCount = (ushort)(entry & 0xffff);
            VolatileShortFlags = (ushort)((entry & 0xffff0000) >> 16);
            VolatileReferenceCount = (ushort)(entry & 0xffff);
        }
    }
    public class MMPFNENTRY
    {
        public bool Modified;
        public bool ReadInProgress;
        public bool WriteInProgress;
        public PageState PageLocation;
        public bool RemovalRequested;
        public uint CacheAttribute;
        public bool OnProtectedStandby;
        public bool ParityError;
        public bool InPageError;
        public uint Priority;

        public MMPFNENTRY(ushort entry)
        {
            Modified = ((entry & 0x10) > 0);
            ReadInProgress = ((entry & 0x20) > 0);
            WriteInProgress = ((entry & 0x8) > 0);
            PageLocation = (PageState)(entry & 0x7);
            RemovalRequested = ((entry & 0x4000) > 0);
            CacheAttribute = (uint)(entry & 0xc0);
            OnProtectedStandby = ((entry & 0x800) > 0);
            ParityError = ((entry & 0x8000) > 0);
            InPageError = ((entry & 0x1000) > 0);
            Priority = (uint)((entry & 0x700) >> 8);
        }
    }
    public enum PageState : uint
    {
        ZeroedPageList = 0,
        FreePageList = 1,
        StandbyPageList = 2,
        ModifiedPageList = 3,
        ModifiedNoWritePageList = 4,
        BadPageList = 5,
        ActiveAndValid = 6,
        TransitionPage = 7
    }
}
