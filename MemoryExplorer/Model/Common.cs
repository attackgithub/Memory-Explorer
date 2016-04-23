using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.ModelObjects;
using MemoryExplorer.Processes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged
    {
        private string ReadString(byte[] buffer, uint offset)
        {
            string result = "";
            while (buffer[offset] != 0)
            {
                result += (char)buffer[offset];
                offset++;
            }
            return result;
        }
        private bool IsValidKernel(string name)
        {
            List<string> KernelNames = new List<string>();
            KernelNames.Add("ntkrnlmp.pdb");
            KernelNames.Add("ntkrnlpa.pdb");
            KernelNames.Add("ntoskrnl.pdb");
            KernelNames.Add("ntkrpamp.pdb");

            return KernelNames.Contains(name);
        }
        private string VersionHelper(string version)
        {
            switch (version)
            {
                case "10.0":
                    return "10.0 (Windows 10)";
                case "6.3":
                    return "6.3 (Windows 8.1 or 2012 R2)";
                case "6.2":
                    return "6.2 (Windows 8 or 2012)";
                case "6.1":
                    return "6.1 (Windows 7 or 2008 R2)";
                case "6.0":
                    return "6.0 (Windows Vista or 2008)";
                case "5.2":
                    return "5.2 (Windows XP x64 or 2003 or 2003 R2)";
                case "5.1":
                    return "5.1 (Windows XP)";
                case "5.0":
                    return "5.0 (Windows 2000)";
                default:
                    return version;
            }
        }
        //private List<LIST_ENTRY> FindAllLists(DataProviderBase dataProvider, LIST_ENTRY source)
        //{
        //    List<LIST_ENTRY> results = new List<LIST_ENTRY>();
        //    List<ulong> seen = new List<ulong>();
        //    List<LIST_ENTRY> stack = new List<LIST_ENTRY>();
        //    AddressBase addressSpace = dataProvider.ActiveAddressSpace;
        //    stack.Add(source);
        //    while (stack.Count > 0)
        //    {
        //        LIST_ENTRY item = stack[0];
        //        stack.RemoveAt(0);
        //        if (!seen.Contains(item.PhysicalAddress))
        //        {
        //            seen.Add(item.PhysicalAddress);
        //            results.Add(item);
        //            ulong Blink = item.Blink;
        //            if (Blink != 0)
        //            {
        //                ulong refr = addressSpace.vtop(Blink);
        //                stack.Add(new LIST_ENTRY(dataProvider, item.Blink));
        //            }
        //            ulong Flink = item.Flink;
        //            if (Flink != 0)
        //            {
        //                ulong refr = addressSpace.vtop(Flink);
        //                stack.Add(new LIST_ENTRY(dataProvider, item.Flink));
        //            }
        //        }
        //    }
        //    return results;
        //}
        private ProcessInfo GetProcessInfo(uint pid, string name)
        {
            foreach (ProcessInfo p in _processList)
            {
                if (p.Pid == pid && p.ProcessName == name)
                    return p;
            }
            return null;
        }
        private ProcessInfo GetProcessInfo(uint pid)
        {
            foreach (ProcessInfo p in _processList)
            {
                if (p.Pid == pid)
                    return p;
            }
            return null;
        }
    }
}
