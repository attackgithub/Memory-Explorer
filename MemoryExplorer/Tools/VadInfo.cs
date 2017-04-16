using MemoryExplorer.Data;
using MemoryExplorer.Model;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Tools
{
    public class VadInfo : ToolBase
    {
        private ProcessInfo _processInfo;
        private ulong _pid;
        private MmVadBase _vadItem;
        private HashSet<ulong> _seen = new HashSet<ulong>();
        public VadInfo(DataModel model, ulong pid) : base(model)
        {
            _pid = pid;
            // check pre-reqs  
            if (_profile == null || _model.KernelBaseAddress == 0 || model.KernelAddressSpace == null)
                throw new ArgumentException("Missing Prerequisites");
        }
        public void Run()
        {
            try
            {
                if (0 == _pid)
                    return;
                _isx64 = (_profile.Architecture == "AMD64");
                ////_processInfo = _profile.Model.FindProcess(_pid);
                if (null == _processInfo)
                    return;
                EProcess_deprecated ep = new EProcess_deprecated(_model, _processInfo.VirtualAddress);
                ulong vadRoot = ep.Members.VadRoot & 0xffffffffffff;
                Traverse(vadRoot, 0);
            }
            catch { }
        }
        private void Traverse(ulong vadRoot, int depth)
        {
            try
            {
                // just for safety, check how deep the tree is
                if (depth > 50)
                    return;
                // some node addresses are not PoolAligned
                // need to see if this is some kind of flag that indicates the entry is no longer in use??
                if (vadRoot % _profile.PoolAlign > 0)
                    return;
                _seen.Add(vadRoot);
                RtlBalancedNode node = new RtlBalancedNode(_model, virtualAddress: vadRoot);
                // the node will map to one of the MMVAD types
                // these change depending on the version of Windows
                // I can use the _POOL_HEADER to determine the type from the pool tag
                bool longExists = false;
                string tagType;
                try
                {
                    long s = _profile.GetStructureSize("_MMVAD_LONG");
                    longExists = (s != -1);
                }
                catch { }
                switch(node.Tag)
                {
                    case "Vad":
                        tagType = "_MMVAD";
                        break;
                    case "Vadl":
                    case "Vadm":
                        tagType = longExists ? "_MMVAD_LONG" : "_MMVAD";
                        break;
                    case "VadS":
                    case "VadF":
                        tagType = "_MMVAD_SHORT";
                        break;
                    default:
                        throw new ArgumentException("Unable to determine TagType from POOL_HEADER");
                }
                if(tagType == "_MMVAD")
                {
                    _vadItem = new MmVad(_model, virtualAddress: vadRoot);
                    MmVad output = _vadItem as MmVad;
                    string pr = output.Core.Protection.ToString();
                    string result = "0x" + output.VirtualAddress.ToString("x12");
                    result += "\t" + depth.ToString();
                    result += "\t0x" +((ulong)output.Core.Members.StartingVpn * 0x1000).ToString("x12");
                    result += "\t0x" + ((ulong)output.Core.Members.EndingVpn * 0x1000 + 0xfff).ToString("x12");
                    result += "\t0x" + (output.Members.FirstPrototypePte & 0xffffffffffff).ToString("x12");
                    result += "\t0x" +(output.Members.LastContiguousPte & 0xffffffffffff).ToString("x12");
                    if (output.Core.Commit < 0x7fffffff)
                        result += "\t" +output.Core.Commit.ToString();
                    else
                        result += "\t -1";
                    result += output.Core.PrivateMemory ? "\tPrivate" : "\tMapped";
                    result += pr.Contains("EXECUTE") ? "\texe\t" : "\t   \t";
                    result += pr;
                    result += "\t" +output.Core.Type.ToString();
                    if (output.Filename != "")
                        result += "\t" + output.Filename;
                    Debug.WriteLine(result);
                    byte[] vadNodes = output.Core.Members.VadNode;
                    int count = vadNodes.Length / 8;
                    ulong[] nodes = new ulong[count];
                    for (int i = 0; i < count; i++)
                        nodes[i] = BitConverter.ToUInt64(vadNodes, i * 8) & 0xffffffffffff;
                    foreach (var item in nodes)
                    {
                        if (item != 0 && !_seen.Contains(item))
                            Traverse(item, depth + 1);
                    }
                }
                else if(tagType == "_MMVAD_SHORT")
                {
                    _vadItem = new MmVadShort(_model, virtualAddress: vadRoot);
                    MmVadShort output = _vadItem as MmVadShort;
                    string pr = output.Protection.ToString();
                    string result = "ﬁx" +output.VirtualAddress.ToString("x12");
                    result += "\t" + depth.ToString();
                    result += "\t0x" + ((ulong)output.Members.StartingVpn * 0x1000).ToString("x12");
                    result += "\t®x" + ((ulong)output.Members.EndingVpn * 0x1000 + 0xfff).ToString("X12");
                    if (output.Commit < 0x7fffffff)
                        result += "\t" + output.Commit.ToString();
                    else
                        result += "\t-1";
                    result += output.PrivateMemory ? "\tPrivate" : "\tMapped";
                    result += pr.Contains("EXECUTE") ? "\texe\t" : "\t   \t";
                    result += pr;
                    result += "\t" + output.Type.ToString();
                    Debug.WriteLine(result);
                    byte[] vadNodes = output.Members.VadNode;
                    int count = vadNodes.Length / 8;
                    ulong[] nodes = new ulong[count];
                    for (int i = 0; i < count; i++)
                        nodes[i] = BitConverter.ToUInt64(vadNodes, i * 8) & 0xffffffffffff;
                    foreach (var item in nodes)
                    {
                        if (item != 0 && !_seen.Contains(item))
                            Traverse(item, depth + 1);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}







