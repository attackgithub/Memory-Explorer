using PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalProcessor
{
    public class Processor : IProcessor
    {
        public string Name
        {
            get
            {
                return "Local Processor Plugin";
            }
        }

        public void Do()
        {
            MessageBox.Show("Do Something in Processor Plugin");
        }
    }
}
