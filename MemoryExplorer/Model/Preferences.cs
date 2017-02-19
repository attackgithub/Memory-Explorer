using MemoryExplorer.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public partial class DataModel : INotifyPropertyChanged, IDisposable
    {
        public void ShowPreferences()
        {
            WriteToLogfile("Show me the preferences");
            Hashtable prefs = new Hashtable();
            prefs.Add("profileCacheLocation", Properties.Settings.Default.ProfileCacheLocation);
            PreferenceViewer preferenceDialog = new PreferenceViewer(prefs);
            if (preferenceDialog.ShowDialog() == true)
            {
                WriteToLogfile("It's all true");
                Properties.Settings.Default.ProfileCacheLocation = preferenceDialog.CacheLocation;
                Properties.Settings.Default.Save();

                _profileCacheLocation = Properties.Settings.Default.ProfileCacheLocation;
            }
        }
    }
}
