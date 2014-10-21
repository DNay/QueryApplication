using System.IO;
using System.Windows;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App ()
        {
            if (Directory.Exists("xulrunner//"))
                Gecko.Xpcom.Initialize("xulrunner//");
        }
    }
}
