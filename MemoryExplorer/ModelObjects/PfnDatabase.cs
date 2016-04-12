using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.ModelObjects
{
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

        public MMPFN(byte[] buffer, int offset)
        {
            GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _MMPFN pfnRecord = (_MMPFN)Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset), typeof(_MMPFN));
            Lock = pfnRecord.Lock;
            PteAddress = pfnRecord.PteAddress & 0xffffffffffff;
            PteLong = pfnRecord.PteLong;
            VolatilePteAddress = pfnRecord.VolatilePteAddress;
            NodeBlinkLow = pfnRecord.NodeBlinkLow;
            VaType = pfnRecord.VaType;
            NodeFlinkLow = pfnRecord.NodeFlinkLow;
            ViewCount = pfnRecord.ViewCount;

            U1 = new u1(pfnRecord.u1);
            U2 = new u2(pfnRecord.u2);
            U3 = new u3(pfnRecord.u3);
            OriginalPte = pfnRecord.OriginalPte;
            U4 = new u4(pfnRecord.u4);
            E1 = pfnRecord.e1;
            E2 = pfnRecord.e2;
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
        public MMPFNENTRY(ushort entry)
        {

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
