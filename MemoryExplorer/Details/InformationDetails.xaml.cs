using MemoryExplorer.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MemoryExplorer.Details
{
    /// <summary>
    /// Interaction logic for InformationDetails.xaml
    /// </summary>
    public partial class InformationDetails : UserControl
    {
        InformationDetailsViewModel _idvm = null;
        public InformationDetails()
        {
            InitializeComponent();
            _idvm = this.DataContext as InformationDetailsViewModel;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                int r = 0;
            }
        }

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                KvpResult kvp = item as KvpResult;
                if(kvp != null)
                {
                    InfoHelper helper = kvp.Helper as InfoHelper;
                    helper.Type = InfoHelperType.InfoDictionary;
                    _idvm.NewSelection(helper);
                }
                    
            }       
        }
    }
}
