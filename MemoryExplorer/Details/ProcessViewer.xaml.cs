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
    /// Interaction logic for ProcessViewer.xaml
    /// </summary>
    public partial class ProcessViewer : UserControl
    {
        ProcessViewerViewModel _pvvm = null;
        public ProcessViewer()
        {
            InitializeComponent();
            _pvvm = this.DataContext as ProcessViewerViewModel;
        }

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                PsListResult result = item as PsListResult;
                if (result != null)
                {
                    InfoHelper helper = result.Helper as InfoHelper;
                    _pvvm.NewSelection(helper);
                }

            }
        }
    }
}
