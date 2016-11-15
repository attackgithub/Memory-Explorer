using MemoryExplorer.Dialogs;
using System;
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
            PreferenceViewer preferenceDialog = new PreferenceViewer("Please enter your name:", Properties.Settings.Default.CacheLocation);
            if (preferenceDialog.ShowDialog() == true)
            {
                WriteToLogfile("It's all true");
                Properties.Settings.Default.CacheLocation = preferenceDialog.Answer;
                Properties.Settings.Default.Save();
            }
        }
    }
}
