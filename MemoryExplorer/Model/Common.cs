using MemoryExplorer.Address;
using MemoryExplorer.Data;
using MemoryExplorer.HexView;
using MemoryExplorer.Info;
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
        public void NewSelection(InfoHelper helper)
        {
            switch(helper.Type)
            {
                case InfoHelperType.DriverObject:
                    helper.BufferSize = DriverObjectSize;
                    UpdateInfoViewer(helper);
                    break;
                case InfoHelperType.InfoDictionary:
                    UpdateInfoViewer(helper);
                    break;
                case InfoHelperType.ProcessInfoDictionary:
                    UpdateInfoViewer(helper);
                    break;
                case InfoHelperType.HandleTable:
                    helper.BufferSize = HandleTableSize;
                    UpdateInfoViewer(helper);
                    break;                    
                case InfoHelperType.ProcessObject:
                    helper.BufferSize = EprocessSize;
                    UpdateInfoViewer(helper);
                    break;
                default:
                    break;
            }
            TellMeAbout(helper);
        }
        private void UpdateInfoViewer(InfoHelper helper)
        {
            if (helper.PhysicalAddress != 0 && helper.BufferSize != 0)
            {
                CurrentInfoHexViewerContentAddress = helper.PhysicalAddress;
                CurrentInfoHexViewerContent = _dataProvider.ReadPhysicalMemory(helper.PhysicalAddress, helper.BufferSize);
            }
            else if (helper.VirtualAddress != 0 && helper.BufferSize != 0)
            {
                CurrentInfoHexViewerContentAddress = helper.VirtualAddress;
                CurrentInfoHexViewerContent = _dataProvider.ReadMemoryBlock(helper.VirtualAddress, helper.BufferSize);
            }
            else
            {
                CurrentInfoHexViewerContentAddress = 0;
                CurrentInfoHexViewerContent = null;
            }
        }        
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
        public void AddInfoHighlight(HexViewHighlight highlight)
        {
            _infoHexHighlights.Add(highlight);
            NotifyPropertyChange("CurrentInfoHexViewerHighlight");
        }
        public void ClearInfoHighlights()
        {
            _infoHexHighlights.Clear();
            NotifyPropertyChange("CurrentInfoHexViewerHighlight");
        }
        public string GetObjectName(ulong index)
        {
            try
            {
                foreach (ObjectTypeRecord t in ObjectTypeList)
                {
                    if (t.Index == index)
                        return t.Name;
                }
                return "--";
            }
            catch { return "--"; }
        }
    }
}
