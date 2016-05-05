using MemoryExplorer.Info;
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
        private void TellMeAbout(InfoHelper helper)
        {
            ClearInfoTree();
            switch (helper.Type)
            {
                case InfoHelperType.InfoDictionary:
                    TellMeAboutTitle = helper.Title;
                    break;
                case InfoHelperType.DriverObject:
                    TellMeAboutTitle = helper.Title;
                    PopulateInfoTree("_DRIVER_OBJECT");
                    break;
                case InfoHelperType.ProcessObject:
                    TellMeAboutTitle = helper.Title;
                    PopulateInfoTree("_EPROCESS");
                    break;
                default:
                    TellMeAboutTitle = "";
                    break;
            }
        }
    }
}
