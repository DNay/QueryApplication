using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Skybound.Gecko;
using Gecko;

namespace QuerySettingApplication
{
    public partial class BrowserWindow : Form
    {
        public BrowserWindow()
        {
            InitializeComponent();
        }

        public GeckoWebBrowser Browser 
        {
            get { return browser; }
        }
    }
}
