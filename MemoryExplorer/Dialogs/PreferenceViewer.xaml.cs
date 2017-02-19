using System;
using System.Collections;
using System.Windows;
using System.Windows.Forms;

namespace MemoryExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for PreferenceViewer.xaml
    /// </summary>
    public partial class PreferenceViewer : Window
    {
        public PreferenceViewer(Hashtable preferences)
        {
            InitializeComponent();
            
            txtCache.Text = preferences["profileCacheLocation"].ToString();
        }
        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //txtCache.SelectAll();
            //txtCache.Focus();
        }

        public string CacheLocation
        {
            get { return txtCache.Text; }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openPicker = new FolderBrowserDialog();

            DialogResult result = openPicker.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                txtCache.Text = openPicker.SelectedPath;
            }
        }
    }
}
