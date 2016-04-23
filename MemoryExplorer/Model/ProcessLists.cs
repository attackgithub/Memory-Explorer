using MemoryExplorer.Processes;
using MemoryExplorer.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged, IDisposable
    {
        async private Task PsList_Method1()
        {
            await Task.Run(() =>
            {
                try
                {
                    PsList1 psList = new PsList1(_profile, _dataProvider);
                    HashSet<ulong> results = psList.Run();
                    foreach (ulong address in results)
                    {
                        EProcess ep = new EProcess(_profile, _dataProvider, address);
                        string name = ep.ImageFileName;
                        uint pid = ep.Pid;
                        if (pid == 0 || name == "")
                            continue;
                        ProcessInfo p = GetProcessInfo(pid, name);
                        if (p == null)
                        {
                            p = new ProcessInfo();
                            p.AddressSpace = _kernelAddressSpace;
                            p.ProcessName = name;
                            p.Pid = pid;
                            p.Dtb = ep.DTB;
                            p.ParentPid = ep.Ppid;
                            p.ActiveThreads = ep.ActiveThreads;
                            p.Session = ep.Session;
                            p.StartTime = ep.StartTime;
                            p.ExitTime = ep.ExitTime;
                            p.ObjectTableAddress = ep.ObjectTable;
                            p.FoundByMethod1 = true;
                            p.PhysicalAddress = ep.PhysicalAddress;
                            p.VirtualAddress = ep.VirtualAddress;
                            AddProcess(p);
                        }
                        else
                        {
                            p.FoundByMethod1 = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return;
                }
            });
        }
        async private Task PsList_Method2()
        {
            await Task.Run(() =>
            {
                try
                {
                    PsList2 psList = new PsList2(_profile, _dataProvider, _processList);
                    HashSet<ulong> results = psList.Run();
                    foreach (ulong address in results)
                    {
                        EProcess ep = new EProcess(_profile, _dataProvider, address);
                        string name = ep.ImageFileName;
                        uint pid = ep.Pid;
                        if (pid == 0 || name == "")
                            continue;
                        ProcessInfo p = GetProcessInfo(pid, name);
                        if (p == null)
                        {
                            p = new ProcessInfo();
                            p.AddressSpace = _kernelAddressSpace;
                            p.ProcessName = name;
                            p.Pid = pid;
                            p.Dtb = ep.DTB;
                            p.ParentPid = ep.Ppid;
                            p.ActiveThreads = ep.ActiveThreads;
                            p.Session = ep.Session;
                            p.StartTime = ep.StartTime;
                            p.ExitTime = ep.ExitTime;
                            p.ObjectTableAddress = ep.ObjectTable;
                            p.FoundByMethod2 = true;
                            p.PhysicalAddress = ep.PhysicalAddress;
                            p.VirtualAddress = ep.VirtualAddress;
                            AddProcess(p);
                        }
                        else
                        {
                            p.FoundByMethod2 = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return;
                }
            });
        }
    }
}
