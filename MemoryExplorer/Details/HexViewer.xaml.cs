using MemoryExplorer.HexView;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Controls;

namespace MemoryExplorer.Details
{
    /// <summary>
    /// Interaction logic for HexViewer.xaml
    /// </summary>
    public partial class HexViewer : UserControl
    {
        HexViewerViewModel _hvvm = null;
        HexViewerControl _hexBoxViewer;

        public HexViewer()
        {
            InitializeComponent();
            this.DataContext = new HexViewerViewModel();
            _hvvm = this.DataContext as HexViewerViewModel;

            _hexBoxViewer = new HexViewerControl();
            _hexBoxViewer.BackColor = System.Drawing.Color.WhiteSmoke;
            _hexBoxViewer.BytesPerLine = 16;
            _hexBoxViewer.ColumnInfoVisible = true;
            _hexBoxViewer.LineInfoVisible = true;
            _hexBoxViewer.StringViewVisible = true;
            _hexBoxViewer.UseFixedBytesPerLine = true;
            _hexBoxViewer.ShadowSelectionVisible = true;
            _hexBoxViewer.VScrollBarVisible = true;
            _hexBoxViewer.ReadOnly = true;
            _hexBoxViewer.HexCasing = HexCasing.Lower;
            _hexBoxViewer.Font = new Font("Courier New", 10.0F, System.Drawing.FontStyle.Regular);
            _hexBoxViewer.SelectionLengthChanged += new System.EventHandler(hexBox_LengthChanged);
            _hexBoxViewer.SelectionStartChanged += new System.EventHandler(hexBox_StartChanged);

            hexView.Child = _hexBoxViewer;

            _hvvm.DataModel.PropertyChanged += HexViewModelPropertyChangedEventHandler;
        }

        private void hexBox_StartChanged(object sender, EventArgs e)
        {
            HexViewerControl hv = sender as HexViewerControl;
            //HexViewModel hvm = ForView.Unwrap<HexViewModel>(hexView.DataContext);
            //hvm.SetStart((ulong)hv.SelectionStart);
            //Debug.WriteLine("Start: " + hv.SelectionStart.ToString());
        }

        private void hexBox_LengthChanged(object sender, EventArgs e)
        {
            HexViewerControl hv = sender as HexViewerControl;
            //HexViewModel hvm = ForView.Unwrap<HexViewModel>(hexView.DataContext);
            //hvm.SetLength((ulong)hv.SelectionLength);
            //Debug.WriteLine("Selected Length: " + hv.SelectionLength.ToString());
        }

        private void HexViewModelPropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            //MainWindowViewModel mwvm = this.DataContext as MainWindowViewModel;
            Debug.WriteLine("Event: " + e.PropertyName);
            if (e.PropertyName == "CurrentHexViewerContent")
            {
                _hexBoxViewer.LineInfoOffset = _hvvm.ActiveStartAddress;
                _hexBoxViewer.ByteProvider = new DynamicByteProvider(_hvvm.DataProvider);
            }
        }

        private void hexEight_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _hexBoxViewer.BytesPerLine = 8;
            _hexBoxViewer.UseFixedBytesPerLine = true;

        }

        private void hexSixteen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _hexBoxViewer.BytesPerLine = 16;
            _hexBoxViewer.UseFixedBytesPerLine = true;
        }

        private void hexAuto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _hexBoxViewer.UseFixedBytesPerLine = false;
        }

        private void clearHighlights_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _hexBoxViewer.ClearHighlights();
        }
    }
}
