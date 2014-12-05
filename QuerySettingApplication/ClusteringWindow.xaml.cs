using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using QuerySettingApplication.Annotations;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for ClusteringWindow.xaml
    /// </summary>
    public partial class ClusteringWindow : Window, INotifyPropertyChanged
    {
        public ClusteringWindow()
        {
            InitializeComponent();
            DataContext = this;
            ServiceSingletons.ClusterWindow = this;
        }

        private void InitializeButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var num = int.Parse(NumInOneClusterText.Text);
                if (num < 1)
                    return;
                ServiceSingletons.ClusterService.Initialize(ServiceSingletons.QueryProcessor.Graph, num);
                UpdateGroupsGraph();
                SSG.IsEnabled = true;
            }
            catch (Exception)
            {
            }
        }

        private void UpdateGroupsGraph()
        {
            foreach (var vertex in ServiceSingletons.QueryProcessor.Graph.Vertexes)
            {
                vertex.Cluster = ServiceSingletons.ClusterService.GetContainigCluster(vertex.Id);
            }
            RaisePropertyChanged("TreeItems");
        }

        public void SetModularity(double currM)
        {
            ModularityText = currM.ToString();
        }

        private void SSG_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                ServiceSingletons.ClusterService.SSG();
                UpdateGroupsGraph();
            });
            RefCG.IsEnabled = true;
        }

        private int i = 0;
        private void RefCG_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                //ServiceSingletons.ClusterService.CG();
                UpdateGroupsGraph();
            });
        }

        private string modularityText = "0";
        public string ModularityText
        {
            get { return modularityText; }
            set
            {
                modularityText = value;
                RaisePropertyChanged("ModularityText");
            }
        }

        public ObservableCollection<TreeViewItem> TreeItems
        {
            get
            {
                //build tree
                var res = new ObservableCollection<TreeViewItem>();

                if (ServiceSingletons.ClusterService.NumClusters() == 0)
                    return res;

                var clusters = new TreeViewItem[ServiceSingletons.ClusterService.NumClusters()];
                
                for (int i = 0; i < ServiceSingletons.ClusterService.NumClusters(); i++)
                    clusters[i] = new TreeViewItem();

                foreach (var vertex in ServiceSingletons.QueryProcessor.Graph.Vertexes)
                {
                     var item = new TreeViewItem();
                     item.Header = vertex.Texts.FirstOrDefault();
                     clusters[ServiceSingletons.ClusterService.GetContainigCluster(vertex.Id)].Items.Add(item);
                }

                for (int i = 0; i < ServiceSingletons.ClusterService.NumClusters(); i++)
                {
                    clusters[i].Header = "Cluster " + i + " (" + clusters[i].Items.Count + " items)";

                    res.Add(clusters[i]);
                }

                return res;
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void ClusterTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sel = ClusterTree.SelectedItem as TreeViewItem;
            if (sel != null && (sel.Header as string).Contains("http"))
                System.Diagnostics.Process.Start(sel.Header as string);
        }

        private void DrawInBrowser_OnClick(object sender, RoutedEventArgs e)
        {
            var br = new BrowserWindow();
            br.Show();
            var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.Graph);
            var fileStreamJ = File.CreateText("page//graph2.json");
            fileStreamJ.Write(strJ);
            fileStreamJ.Close();
            var path = "file:///" + Directory.GetCurrentDirectory() + "\\page\\index.html";
            br.Browser.Navigate(path);
        }
    }
}
