using MemoryExplorer.Worker;
using PluginContracts;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void LoadPlugin(ref Job j)
        {
            try
            {
                _plugin = null;
                var pluginLocation = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                Debug.WriteLine("The processor is loading plugin " + j.ActionMessage[0]);
                AssemblyName an = AssemblyName.GetAssemblyName(Path.Combine(pluginLocation, j.ActionMessage[0] + ".dll"));
                Debug.WriteLine("Plugin: " + an.FullName);
                _pluginAssembly = Assembly.Load(an);
                Type[] types = _pluginAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsInterface || type.IsAbstract) { continue; }
                    if (type.FullName == "LocalProcessor.Processor") // have have an interface compatible plugin
                    {
                        _plugin = Activator.CreateInstance(type) as IProcessor;
                        if (_plugin != null)
                        {
                            j.Status = JobStatus.Complete;
                            Debug.WriteLine("Loaded Plugin Says: " + _plugin.Name);
                        }
                        else
                        {
                            j.Status = JobStatus.Failed;
                            j.ErrorMessage = "Incompatible Plugin";
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
    }
}
