using MemoryExplorer.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryExplorer
{
    public class BindableBase : INotifyPropertyChanged
    {
        protected static DataModel _dataModel = null;

        public BindableBase()
        {
            if(_dataModel != null)
                _dataModel.PropertyChanged += DataModelPropertyChanged;

        }

        protected virtual void SetProperty<T>(ref T member, T val,
            [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(member, val)) return;

            member = val;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void DataModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

    }
}
