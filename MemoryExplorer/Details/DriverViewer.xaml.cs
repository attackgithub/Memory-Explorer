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
    /// Interaction logic for DriverViewer.xaml
    /// </summary>
    public partial class DriverViewer : UserControl
    {
        DriverViewerViewModel _dvvm = null;
        public DriverViewer()
        {
            InitializeComponent();
            _dvvm = this.DataContext as DriverViewerViewModel;
        }

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                DriverResult result = item as DriverResult;
                if (result != null)
                {
                    InfoHelper helper = result.Helper as InfoHelper;
                    _dvvm.NewSelection(helper);
                }

            }
        }
    }
}
