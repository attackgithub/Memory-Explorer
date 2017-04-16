using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using MemoryExplorer.Worker;
using Pdb_Magician;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        public void GetProfileIdentifier(ref Job j)
        {
            bool missingProfile = false;
            string archiveFile = Path.Combine(_dataProvider.CacheFolder, "1001.dat");
            FileInfo fi = new FileInfo(archiveFile);
            if (fi.Exists)
            {
                string[] items = File.ReadAllLines(archiveFile);
                foreach (string item in items)
                {
                    j.ActionMessage.Add(item);
                    string profileCache = Path.Combine(_model.ProfileCacheLocation, item);
                    if (!new DirectoryInfo(profileCache).Exists)
                    {
                        missingProfile = true;
                    }
                }
                if (!missingProfile)
                {
                    j.Status = JobStatus.Complete;
                    return;
                }
            }
            StringSearch mySearch = new StringSearch(_dataProvider);
            PdbMagician magician = new PdbMagician();
            List<string> todoList = new List<string>();
            todoList.Add("_EPROCESS");
            todoList.Add("_KUSER_SHARED_DATA");
            todoList.Add("_OBJECT_TYPE");
            todoList.Add("_OBJECT_HEADER");
            todoList.Add("_OBJECT_DIRECTORY_ENTRY");
            todoList.Add("_OBJECT_DIRECTORY");
            todoList.Add("_OBJECT_HEADER_CREATOR_INFO");
            todoList.Add("_OBJECT_HEADER_NAME_INFO");
            todoList.Add("_OBJECT_HEADER_HANDLE_INFO");
            todoList.Add("_OBJECT_HEADER_QUOTA_INFO");
            todoList.Add("_OBJECT_HEADER_PROCESS_INFO");
            todoList.Add("_OBJECT_HEADER_AUDIT_INFO");
            int successCount = 0;
            mySearch.AddNeedle("RSDS");
            foreach (var answer in mySearch.Scan())
            {
                try
                {
                    List<ulong> hitList = answer["RSDS"];
                    foreach (ulong hit in hitList)
                    {
                        RSDS rsds = new RSDS(_dataProvider, hit);
                        if (rsds.Signature == "RSDS" && (rsds.Filename == "ntkrnlpa.pdb" || rsds.Filename == "ntkrnlmp.pdb" || rsds.Filename == "ntkrpamp.pdb" || rsds.Filename == "ntoskrnl.pdb"))
                        {

                            string profileCache = Path.Combine(_model.ProfileCacheLocation, rsds.GuidAge);
                            DirectoryInfo di = new DirectoryInfo(profileCache);
                            if (!di.Exists)
                            {
                                bool result = magician.RetrieveSymbolFile(rsds.Filename, rsds.GuidAge, _model.ProfileCacheLocation);
                                string targetPdb = Path.Combine(Path.Combine(_model.ProfileCacheLocation, rsds.GuidAge), rsds.Filename);
                                fi = new FileInfo(targetPdb);
                                if (result && fi.Exists)
                                {
                                    result = magician.ParseSymbolFile(targetPdb, profileCache, todoList.ToArray());
                                    if (result && !j.ActionMessage.Contains(rsds.GuidAge))
                                    {
                                        successCount++;
                                        j.ActionMessage.Add(rsds.GuidAge);
                                    }
                                }
                            }
                            else if (!j.ActionMessage.Contains(rsds.GuidAge))
                            {
                                successCount++;
                                j.ActionMessage.Add(rsds.GuidAge);
                            }
                            Debug.WriteLine(hit.ToString("X8") + "\t" + rsds.Filename + "\t" + rsds.GuidAge);
                        }
                    }

                }
                catch { }
            }
            if (successCount > 0)
            {
                j.Status = JobStatus.Complete;
                File.WriteAllLines(Path.Combine(_dataProvider.CacheFolder, "1001.dat"), j.ActionMessage);
            }
            else
                j.Status = JobStatus.Failed;


            //Dictionary<string, object> info = _dataProvider.GetInformation();
            //string friendlyKey;
            //foreach (var item in info)
            //{
            //    if (item.Key == "dtb")
            //    {
            //        friendlyKey = "Directory Table Base";
            //        //_model._kernelDtb = (ulong)item.Value;
            //    }
            //    else if (item.Key == "maximumPhysicalAddress")
            //        friendlyKey = "Maximum Physical Address";
            //}
        }
    }
}
