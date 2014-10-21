using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace QuerySettingApplication
{
    public class JenaFusekiHelper
    {
        private string _fusekiFolder = "jena-fuseki-1.1.1\\";

        public void StartServer()
        {
            if (!Directory.Exists(_fusekiFolder))
            {
                var dlg = new CommonOpenFileDialog();
                dlg.Title = "Choose fuseki folder";
                dlg.IsFolderPicker = true;

                dlg.AddToMostRecentlyUsedList = false;
                dlg.AllowNonFileSystemItems = false;
                dlg.DefaultDirectory = _fusekiFolder;
                dlg.EnsureFileExists = true;
                dlg.EnsurePathExists = true;
                dlg.EnsureReadOnly = false;
                dlg.EnsureValidNames = true;
                dlg.Multiselect = false;
                dlg.ShowPlacesList = true;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    if (dlg.FileName != null)
                        _fusekiFolder = dlg.FileName;
            }

            if (!Directory.Exists(_fusekiFolder))
                return;

            var pRun = new Process();
            pRun.EnableRaisingEvents = true;
            pRun.StartInfo.WorkingDirectory = _fusekiFolder;
            pRun.StartInfo.FileName = "fuseki-server.bat";
            pRun.StartInfo.Arguments = "--config=config.ttl";
            pRun.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            pRun.Start();
        }

        public void SendQuery(string command)
        {
            var procInfo = new ProcessStartInfo("cmd", "/c" + command);
            procInfo.WorkingDirectory = _fusekiFolder;
            procInfo.UseShellExecute = false;
            procInfo.CreateNoWindow = true;

            var proc = new Process();
            proc.StartInfo = procInfo;
            proc.Exited += ProcOnExited;//new EventHandler(ServiceSingletons.MainWindow.OnQueryProcExited);
            proc.Start();
            
            proc.WaitForExit();
        }

        private void ProcOnExited(object sender, EventArgs eventArgs)
        {
            int a = 0;
        }
    }
}
