using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for ClusteringWindow.xaml
    /// </summary>
    public partial class ClusteringWindow : Window, INotifyPropertyChanged
    {
        private readonly Graph _graph;

        public ClusteringWindow(Graph graph)
        {
            _graph = graph;
            InitializeComponent();
            DataContext = this;
            ServiceSingletons.ClusterWindow = this;
            RaisePropertyChanged("TreeItems");
        }

        private void InitializeButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var num = int.Parse(NumInOneClusterText.Text);
                if (num < 1)
                    return;
                ServiceSingletons.ClusterService.Initialize(_graph, num);
                UpdateGroupsGraph();
                SSG.IsEnabled = true;
            }
            catch (Exception)
            {
            }
        }

        private void UpdateGroupsGraph()
        {
            foreach (var vertex in _graph.Vertexes)
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

        private void RefCG_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                ServiceSingletons.ClusterService.CG();
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

                if (_graph.Vertexes.Count == 0)
                    return res;

                var numCl = _graph.Vertexes.Select(t => t.Cluster).Max() + 1;

                var clusters = new TreeViewItem[numCl];

                for (int i = 0; i < numCl; i++)
                    clusters[i] = new TreeViewItem();

                foreach (var vertex in _graph.Vertexes)
                {
                     var item = new TreeViewItem();
                     item.Header = vertex.Texts.FirstOrDefault();
                     clusters[vertex.Cluster].Items.Add(item);
                }

                for (int i = 0; i < numCl; i++)
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

        private void PrepareJsonData()
        {
            PositingPoints(_graph, UseSpiral.IsChecked);
            CalculateDegree(_graph);
            var strJ = JsonConvert.SerializeObject(_graph);
            var fileStreamJ = File.CreateText("page//graph2.json");
            fileStreamJ.Write(strJ);
            fileStreamJ.Close();
        }

        private void CalculateDegree(Graph graph)
        {
            foreach (var edge in graph.Edges)
            {
                graph.Vertexes[edge.target].degreeIn++;
            }
        }

        private string Path
        {
            get { return "file:///" + Directory.GetCurrentDirectory() + "\\page\\index.html"; }
        }

        private void DrawInApplication_OnClick(object sender, RoutedEventArgs e)
        {
            PrepareJsonData();
            var br = new BrowserWindow();
            br.Show();
            br.Browser.Navigate(Path);
        }

        private void DrawInBrowser_OnClick(object sender, RoutedEventArgs e)
        {
            PrepareJsonData();
            var firefoxPath = "\"c:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe\" ";
            try
            {
                System.Diagnostics.Process.Start(firefoxPath + Path);
            }
            catch (Exception)
            {
                System.Diagnostics.Process.Start(Path);
            }
            
        }

        private static void PositingPoints(Graph graph, bool? isSpiral)
        {
            var clusters = new Dictionary<int, int>();
            var clustersAdded = new Dictionary<int, int>();
            foreach (var vertex in graph.Vertexes)
            {
                if (!clusters.ContainsKey(vertex.Cluster))
                {
                    clusters.Add(vertex.Cluster, 1);
                    clustersAdded.Add(vertex.Cluster, 0);
                }
                else
                    clusters[vertex.Cluster]++;
            }

            var phi = 2*Math.PI/clusters.Count;
            var radMax = 40*Math.Log(clusters.Select(t => t.Value).Max(), 2);
            var radius = 2 * radMax/Math.Sin(phi) + 50;

            foreach (var vertex in graph.Vertexes)
            {
                var centerAngle = phi*vertex.Cluster;
                var centerGroupX = radius*Math.Cos(centerAngle);
                var centerGroupY = radius*Math.Sin(centerAngle);

                var vertexAngle = 2 * Math.PI / clusters[vertex.Cluster] * clustersAdded[vertex.Cluster];
                var vertexRadius = 40 * Math.Log(clusters[vertex.Cluster], 2);

                if (isSpiral.HasValue && isSpiral.Value)
                {
                    vertexAngle *= 2;
                    vertexRadius *= (double)clustersAdded[vertex.Cluster] / clusters[vertex.Cluster];
                }

                vertex.X = centerGroupX + vertexRadius * Math.Cos(vertexAngle);
                vertex.Y = centerGroupY + vertexRadius * Math.Sin(vertexAngle);
                clustersAdded[vertex.Cluster]++;
            }

            foreach (var edge in graph.Edges)
            {
                if (graph.Vertexes[edge.source].Cluster == graph.Vertexes[edge.target].Cluster)
                {
                    var centerAngle = phi * graph.Vertexes[edge.source].Cluster;
                    edge.fictX = radius * Math.Cos(centerAngle);
                    edge.fictY = radius * Math.Sin(centerAngle);
                }
                else
                {
                    edge.fictX = 0;
                    edge.fictY = 0;                
                }
            }
        }
    }
}
