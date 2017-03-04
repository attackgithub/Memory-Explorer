using PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalIngester
{
    public class Ingester : IIngester
    {
        public string Name
        {
            get
            {
                return "Local Ingester Plugin";
            }
        }

        public void Do()
        {
            MessageBox.Show("Do Something in Ingester Plugin");
        }
    }
}
