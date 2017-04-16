using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.Profiles;
using System;

namespace MemoryExplorer.ModelObjects
{
    public class RtlBalancedNode : StructureBase
    {
        private byte _balance;
        private ulong _left;
        private ulong _right;
        private ulong _bodySize;
        private string _tag;

        public RtlBalancedNode(DataModel model, ulong virtualAddress = 0, ulong physicalAddress = 0) : base(model, virtualAddress)
        {
            _physicalAddress = physicalAddress;
            Overlay("_RTL_BALANCED_NODE");
            // the following may well be dodgy - so check the profile description for the balanced node
            // for now I'm assuming the first child maps left and the second child to the right
            byte[] children = Members.Children;
            _balance = Members.Balance;
            if(_is64)
            {
                _left = BitConverter.ToUInt64(children, 0) & 0xffffffffffff;
                _right = BitConverter.ToUInt64(children, 8) & 0xffffffffffff;
            }
            else
            {
                _left = BitConverter.ToUInt64(children, 0);
                _right = BitConverter.ToUInt64(children, 4);
            }
            ulong poolHeaderSize = (ulong)_profile.GetStructureSize("_POOL_HEADER");
            PoolHeader ph = new PoolHeader(_model, virtualAddress: _virtualAddress - poolHeaderSize);
            _tag = ph.Tag;
            _bodySize = (ph.BlockSize * _profile.PoolAlign) - (ulong)ph.Size;
        }
        public ulong Left { get { return _left; } }
        public ulong Right { get { return _right; } }
        public ulong BodySize { get { return _bodySize; } }
        public byte Balance { get { return _balance; } }
        public string Tag { get { return _tag; } }
    }
}
