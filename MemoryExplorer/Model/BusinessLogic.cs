using MemoryExplorer.Profiles;
using MemoryExplorer.Scanners;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged
    {
        async private void InitialSurvey()
        {

            IncrementActiveJobs();
            await GetInformation();
            DecrementActiveJobs();

            IncrementActiveJobs();
            await FindProfileGuid();
            DecrementActiveJobs();


        }
        async private Task GetInformation()
        {
            await Task.Run(() => 
            {
                Dictionary<string, object> info = _dataProvider.GetInformation();
            });
        }
        async private Task FindProfileGuid()
        {
            await Task.Run(() => 
            {
                StringSearch mySearch = new StringSearch(_dataProvider);
                mySearch.AddNeedle("RSDS");
                Dictionary<string, List<ulong>> results = mySearch.Scan();
                foreach (var kvp in results)
                {
                    if (kvp.Key != "RSDS")
                        continue;
                    List<ulong> hitList = kvp.Value;
                    foreach (ulong hit in hitList)
                    {
                        try
                        {
                            RSDS rsds = new RSDS(MemoryImageFilename, hit);
                            if (rsds.Signature == "RSDS" && (rsds.Filename == "ntkrnlpa.pdb" || rsds.Filename == "ntkrnlmp.pdb" || rsds.Filename == "ntkrpamp.pdb"))
                            {
                                ProfileName = rsds.GuidAge + ".gz";
                                AddToInfoDictionary("ProfileName", ProfileName);
                                Profile profile = new Profile(ProfileName, @"E:\Forensics\MxProfileCache"); // TO DO - make this a user option when you get around to writing the settings dialog
                                Architecture = profile.Architecture;
                                AddToInfoDictionary("Architecture", Architecture);
                                break;
                            }
                        }
                        catch { }
                    }
                }
            });
        }
    }
}
