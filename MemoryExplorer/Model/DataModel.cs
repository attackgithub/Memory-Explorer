using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Model
{
    public class DataModel
    {
        #region globals
        private bool _runningAsAdmin = false;
        private bool _liveCapture = false;


        #endregion
        #region access
        public bool RunningAsAdmin { get { return _runningAsAdmin; } }
        public bool LiveCapture
        {
            get { return _liveCapture; }
            set { _liveCapture = value; }
        }

        #endregion
        public DataModel(bool IsAdmin)
        {
            _runningAsAdmin = IsAdmin;
        }

        
    }
}
