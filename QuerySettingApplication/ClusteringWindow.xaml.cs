using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly IGraph _graph;
        private readonly IClusterService _clustService;

        public ClusteringWindow(IGraph graph, IClusterService service)
        {
            _clustService = service;
            _graph = graph;
            InitializeComponent();
            DataContext = this;
            ServiceSingletons.ClusterWindow = this;
            _clustService.SetGraph(graph);
            RaisePropertyChanged("TreeItems");
            RaisePropertyChanged("InfoItems");
            SetModularity(_clustService.WeightOfClustering());
        }

        private void Cluster_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessed)
                return;
            _isProcessed = true;
            //init
            _clustService.Initialize();
            UpdateGroupsGraph();
            SetModularity(_clustService.WeightOfClustering());

            int gMode = SSGMode.IsChecked.HasValue && SSGMode.IsChecked.Value ? 0 : 1;
            int refMode = CGMode.IsChecked.HasValue && CGMode.IsChecked.Value ? 0 : FGMode.IsChecked.HasValue && FGMode.IsChecked.Value ? 1 : 2;
            var text = MergeFactor.Text;

            Task.Factory.StartNew(() => ClusterProcess(gMode, refMode, text));
        }

        private void ClusterProcess(int gMode, int refMode, string mf)
        {
            switch (gMode)
            {
                case 0:
                    _clustService.SSG();
                    break;
                default:
                    {
                        float factor;
                        if (float.TryParse(mf, out factor))
                            _clustService.MSG(factor);
                    }
                    break;
            }

            UpdateGroupsGraph();

            switch (refMode)
            {
                case 0:
                    _clustService.CG();
                    break;
                case 1:
                    _clustService.FG();
                    break;
                default:
                    _clustService.AKL();
                    break;
            }

            UpdateGroupsGraph();
        }

        private void UpdateGroupsGraph()
        {
            foreach (var vertex in _graph.Vertexes)
            {
                vertex.Cluster = _clustService.GetContainigCluster(vertex.Id);
            }
            RaisePropertyChanged("TreeItems");
            RaisePropertyChanged("InfoItems");
            _isProcessed = false;
        }

        public void SetModularity(double currM)
        {
            ModularityText = currM.ToString();
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

        private string squareErrorText = "0";
        private bool _isProcessed;

        public string SquareErrorText
        {
            get { return squareErrorText; }
            set
            {
                squareErrorText = value;
                RaisePropertyChanged("SquareErrorText");
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
                    item.Header = vertex.Name;
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

        internal class ComparerInfo : IComparer<KeyValuePair<RdfInfo, int>>
        {
            public int Compare(KeyValuePair<RdfInfo, int> x, KeyValuePair<RdfInfo, int> y)
            {
                return (-1) * x.Value.CompareTo(y.Value);
            }
        }

        public ObservableCollection<TreeViewItem> InfoItems
        {
            get
            {
                //build tree
                var res = new ObservableCollection<TreeViewItem>();

                if (_graph.Vertexes.Count == 0)
                    return res;

                if (_graph.Vertexes.Any(t => t.Infos == null))
                    return res;

                var numCl = _graph.Vertexes.Select(t => t.Cluster).Max() + 1;

                var clusters = new TreeViewItem[numCl];

                for (int i = 0; i < numCl; i++)
                    clusters[i] = new TreeViewItem();

                var clusterInfos = new Dictionary<RdfInfo, int>[numCl];
                for (int index = 0; index < numCl; index++)
                    clusterInfos[index] = new Dictionary<RdfInfo, int>();
                var entNum = new int[numCl];
                var entNumWithInfo = new int[numCl];

                foreach (var vertex in _graph.Vertexes)
                {
                    var cl = vertex.Cluster;
                    entNum[cl]++;
                    if (vertex.Infos.Any())
                        entNumWithInfo[cl]++;

                    foreach (var info in vertex.Infos)
                    {
                        if (!clusterInfos[cl].ContainsKey(info))
                            clusterInfos[cl].Add(info, 0);
                        clusterInfos[cl][info]++;
                    }
                }

                var errors = new double[numCl];

                foreach (var vertex in _graph.Vertexes)
                {
                    if (!vertex.Infos.Any())
                        continue;

                    var error = 0.0;
                    var cl = vertex.Cluster;

                    foreach (var info in clusterInfos[cl])
                    {
                        var p = (double)info.Value / entNumWithInfo[cl];

                        if (vertex.Infos.Contains(info.Key))
                            error += Math.Pow(1 - p, 2);
                        else
                            error += Math.Pow(p, 2);
                    }

                    errors[cl] += error;
                }

                SquareErrorText = errors.Sum().ToString();

                var sortedClusterInfos = new List<KeyValuePair<RdfInfo, int>>[numCl];

                for (int index = 0; index < clusterInfos.Length; index++)
                {
                    var clusterInfo = clusterInfos[index].ToList();
                    clusterInfo.Sort(new ComparerInfo());

                    sortedClusterInfos[index] = clusterInfo;
                }

                for (int index = 0; index < numCl; index++)
                {
                    var clusterInfo = sortedClusterInfos[index];
                    foreach (var info in clusterInfo)
                    {
                        var item = new TreeViewItem();
                        item.Header = string.Format("{0}% : {1} / {2} : {3} - {4}", (100.0 * info.Value / entNum[index]).ToString("0.00"), info.Value, entNum[index], info.Key.Predicate, info.Key.Subject);
                        clusters[index].Items.Add(item);
                    }
                }

                for (int i = 0; i < numCl; i++)
                {
                    clusters[i].Header = string.Format("Cluster {0} ({1} items), SqError = {2}", i, clusters[i].Items.Count, errors[i].ToString("0.000"));

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

        private void ClusterWithAnotherGraph_OnClick(object sender, RoutedEventArgs e)
        {
            if (_graph == ServiceSingletons.QueryProcessor.CiteNet)
                ClusterizeCiteNetByAuthors(_graph as CiteNet, ServiceSingletons.QueryProcessor.GraphAuthors);
            else
                ClusterizeAuthorsByCiteNet(_graph as AuthorsGraph, ServiceSingletons.QueryProcessor.CiteNet);

            SetModularity(_clustService.WeightOfClustering());
            RaisePropertyChanged("TreeItems");
            RaisePropertyChanged("InfoItems");
        }

        private void ClusterizeAuthorsByCiteNet(AuthorsGraph authorsGraph, CiteNet citeNet)
        {
            var authors = new Dictionary<string, List<string>>();

            foreach (VertexPublication vert in citeNet.Vertexes)
            {
                authors.Add(vert.Name, vert.Authors);
            }

            var authClusters = new Dictionary<string, Dictionary<int, int>>();

            foreach (var vertex in citeNet.Vertexes)
            {
                var publ = vertex.Name;

                if (!authors.ContainsKey(publ))
                    continue;

                foreach (var author in authors[publ])
                {
                    if (!authClusters.ContainsKey(author))
                        authClusters.Add(author, new Dictionary<int, int>());

                    if (!authClusters[author].ContainsKey(vertex.Cluster))
                        authClusters[author].Add(vertex.Cluster, 0);

                    authClusters[author][vertex.Cluster]++;
                }
            }

            foreach (var cluster in authClusters)
            {
                var cl = cluster.Value.Keys.First();
                var max = cluster.Value.Values.First();
                foreach (var pair in cluster.Value)
                {
                    if (max < pair.Value)
                    {
                        cl = pair.Key;
                        max = pair.Value;
                    }
                }

                authorsGraph.GetVertex(cluster.Key).Cluster = cl;
            }
        }

        private void ClusterizeCiteNetByAuthors(CiteNet citeNet, AuthorsGraph authorsGraph)
        {
            var authors = new Dictionary<string, List<string>>();

            foreach (Vertex vert in authorsGraph.Vertexes)
            {
                var publs = citeNet.Vertexes.Where(t => (t as VertexPublication).Authors.Contains(vert.Name)).Select(t => t.Name).ToList();
                authors.Add(vert.Name, publs);
            }

            var citeNetClusters = new Dictionary<string, Dictionary<int, int>>();

            foreach (var vertex in authorsGraph.Vertexes)
            {
                var publ = vertex.Name;

                if (!authors.ContainsKey(publ))
                    continue;

                foreach (var p in authors[publ])
                {
                    if (!citeNetClusters.ContainsKey(p))
                        citeNetClusters.Add(p, new Dictionary<int, int>());

                    if (!citeNetClusters[p].ContainsKey(vertex.Cluster))
                        citeNetClusters[p].Add(vertex.Cluster, 0);

                    citeNetClusters[p][vertex.Cluster]++;
                }
            }

            foreach (var cluster in citeNetClusters)
            {
                var cl = cluster.Value.Keys.First();
                var max = cluster.Value.Values.First();
                foreach (var pair in cluster.Value)
                {
                    if (max < pair.Value)
                    {
                        cl = pair.Key;
                        max = pair.Value;
                    }
                }

                citeNet.GetVertex(cluster.Key).Cluster = cl;
            }

            /*int clusterNum = citeNet.Vertexes.Max(t => t.Cluster);

            foreach (var vert in citeNet.Vertexes.Where(t => !citeNetClusters.ContainsKey(t.Name)))
            {
                vert.Cluster = clusterNum + 1;
            }*/

            //_clustService.Renumber();
        }

        private void PrepareJsonData(DrawModEnum mode)
        {
            var graph = _graph;

            switch (mode)
            {
                case DrawModEnum.ROUND:
                    PositingPointsCircle(graph);
                    break;
                case DrawModEnum.RANDOM:
                    PositingPointsComponentRandom(graph);
                    break;
                case DrawModEnum.TIME:
                    PositingPointsComponentTime(graph);
                    break;
                case DrawModEnum.CLUSTERS:
                    graph = GetClusterGraph();
                    break;
                default:
                    break;
            }

            if (mode != DrawModEnum.CLUSTERS)
                CalculateDegree(graph);

            var strJ = JsonConvert.SerializeObject(graph);
            var fileStreamJ = File.CreateText("page//graph2.json");
            fileStreamJ.Write(strJ);
            fileStreamJ.Close();
        }

        private IGraph GetClusterGraph()
        {
            var res = new Graph<Vertex>();
            for (var i = 0; i < _clustService.NumClusters(); i++)
            {
                var v = res.AddVertex(i.ToString());
                v.Cluster = i;
            }

            foreach (var vertex in _graph.Vertexes)
            {
                var cl = vertex.Cluster;
                res.GetVertex(cl.ToString()).degreeIn++;

                var indV = _clustService.GetIndVertexes(vertex.Id);
                if (indV != null)
                    foreach (var v in indV)
                    {
                        var vcl = _clustService.GetContainigCluster(v);
                        if (vcl == cl)
                            continue;
                        var e = res.AddEdge(new Edge(cl, vcl));
                        e.weight += 0.01;
                    }
            }

            return res;
        }

        private void CalculateDegree(IGraph graph)
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

        private string WPath
        {
            get { return "file:///" + Directory.GetCurrentDirectory() + "\\page\\index3.html"; }
        }

        public object ClusteringWithAnotherEnabled
        {
            get
            {
                if (_graph == ServiceSingletons.QueryProcessor.CiteNet)
                    return ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any();
                else
                    return ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Any();
            }
        }

        private void DrawInBrowserTime_OnClick(object sender, RoutedEventArgs e)
        {
            DrawProcess(DrawModEnum.TIME);
        }

        private void DrawInBrowserRandom_OnClick(object sender, RoutedEventArgs e)
        {
            DrawProcess(DrawModEnum.RANDOM);
        }

        private void DrawInBrowserRound_OnClick(object sender, RoutedEventArgs e)
        {
            DrawProcess(DrawModEnum.ROUND);
        }

        private void DrawInBrowserWeight_OnClick(object sender, RoutedEventArgs e)
        {
            DrawProcess(DrawModEnum.WEIGHT);
        }
        private void DrawInBrowserClusers_OnClick(object sender, RoutedEventArgs e)
        {
            DrawProcess(DrawModEnum.CLUSTERS);
        }

        private void DrawProcess(DrawModEnum mode)
        {
            PrepareJsonData(mode);

            var path = "\"" + Path + "\"";
            if (mode == DrawModEnum.WEIGHT || mode == DrawModEnum.CLUSTERS)
                path = "\"" + WPath + "\"";

            try
            {
                var info = new ProcessStartInfo(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe", path);
                Process.Start(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine(path);
                Console.WriteLine(ex.Message);
                Process.Start(Path);
            }
        }

        private static void PositingPointsCircle(IGraph graph)
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

            var phi = 2 * Math.PI / clusters.Count;
            var radMax = 40 * Math.Log(clusters.Select(t => t.Value).Max(), 2);
            var radius = 2 * radMax / Math.Sin(phi) + 50;

            foreach (var vertex in graph.Vertexes)
            {
                var centerAngle = phi * vertex.Cluster;
                var centerGroupX = radius * Math.Cos(centerAngle) + radMax + radius;
                var centerGroupY = radius * Math.Sin(centerAngle) + radMax + radius;

                var vertexAngle = 2 * Math.PI / clusters[vertex.Cluster] * clustersAdded[vertex.Cluster];
                var vertexRadius = 40 * Math.Log(clusters[vertex.Cluster], 2);

                vertex.X = centerGroupX + vertexRadius * Math.Cos(vertexAngle);
                vertex.Y = centerGroupY + vertexRadius * Math.Sin(vertexAngle);
                clustersAdded[vertex.Cluster]++;
            }

            foreach (var edge in graph.Edges)
            {
                if (graph.Vertexes[edge.source].Cluster == graph.Vertexes[edge.target].Cluster)
                {
                    var centerAngle = phi * graph.Vertexes[edge.source].Cluster;
                    edge.fictX = radius * Math.Cos(centerAngle) + radMax + radius;
                    edge.fictY = radius * Math.Sin(centerAngle) + radMax + radius;
                }
                else
                {
                    edge.fictX = 0 + radMax + radius;
                    edge.fictY = 0 + radMax + radius;
                }
            }
        }

        private static void PositingPointsComponentTime(IGraph graph)
        {
            var citeNet = graph as CiteNet;

            if (citeNet == null)
                return;

            var clusters = new Dictionary<int, int>();
            var clustersAdded = new Dictionary<int, int>();
            var clustersHeight = new Dictionary<int, double>();
            var clustersY = new Dictionary<int, double>();

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

            double h = 0;
            var ran = new Random(DateTime.Now.Millisecond);

            foreach (var cluster in clusters)
            {
                clustersHeight[cluster.Key] = Math.Log(cluster.Value) * 20;

                clustersY[cluster.Key] = h;

                h += clustersHeight[cluster.Key] + 40;
            }

            var maxTime = new DateTime(2015, 1, 1);
            var minTime = new DateTime(1900, 1, 1);
            var xLen = (maxTime - minTime).TotalDays;

            foreach (VertexPublication vertex in graph.Vertexes)
            {
                vertex.X = vertex.Date < minTime ? 0.5 * 4000 : (vertex.Date - minTime).TotalDays / xLen * 4000;
                vertex.Y = clustersY[vertex.Cluster] + ran.NextDouble() * clustersHeight[vertex.Cluster];
                clustersAdded[vertex.Cluster]++;
            }

            foreach (var edge in graph.Edges)
            {
                if (graph.Vertexes[edge.source].Cluster == graph.Vertexes[edge.target].Cluster)
                {
                    if (graph.Vertexes[edge.target].X > graph.Vertexes[edge.source].X)
                        edge.fictX = graph.Vertexes[edge.target].X - 100;
                    else
                        edge.fictX = graph.Vertexes[edge.target].X + 100;
                    edge.fictY = graph.Vertexes[edge.target].Y;
                }
                else
                {
                    edge.fictX = (graph.Vertexes[edge.source].X + graph.Vertexes[edge.target].X) / 2;
                    edge.fictY = (graph.Vertexes[edge.source].Y + graph.Vertexes[edge.target].Y) / 2;
                }
            }
        }

        private static void PositingPointsComponentRandom(IGraph graph)
        {
            var clusters = new Dictionary<int, int>();
            var clustersAdded = new Dictionary<int, int>();
            var clustersHeight = new Dictionary<int, double>();
            var clustersWidth = new Dictionary<int, double>();
            var clustersX = new Dictionary<int, double>();
            var clustersY = new Dictionary<int, double>();

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

            double h = 0;
            var ran = new Random(DateTime.Now.Millisecond);

            foreach (var cluster in clusters)
            {
                clustersWidth[cluster.Key] = Math.Log(cluster.Value) * 100;
                clustersHeight[cluster.Key] = Math.Log(cluster.Value) * 40;

                clustersY[cluster.Key] = h;
                h += clustersHeight[cluster.Key];
                clustersX[cluster.Key] = ran.NextDouble() * 4000;
            }

            foreach (var vertex in graph.Vertexes)
            {
                vertex.X = clustersX[vertex.Cluster] + ran.NextDouble() * clustersWidth[vertex.Cluster];
                vertex.Y = clustersY[vertex.Cluster] + ran.NextDouble() * clustersHeight[vertex.Cluster];
                clustersAdded[vertex.Cluster]++;
            }

            foreach (var edge in graph.Edges)
            {
                if (graph.Vertexes[edge.source].Cluster == graph.Vertexes[edge.target].Cluster)
                {
                    edge.fictX = clustersX[graph.Vertexes[edge.source].Cluster] + clustersWidth[graph.Vertexes[edge.source].Cluster] * 0.5;
                    edge.fictY = clustersY[graph.Vertexes[edge.source].Cluster] + clustersHeight[graph.Vertexes[edge.source].Cluster] * 0.5;
                }
                else
                {
                    edge.fictX = (clustersX[graph.Vertexes[edge.source].Cluster] + clustersX[graph.Vertexes[edge.target].Cluster]) / 2;
                    edge.fictY = (clustersY[graph.Vertexes[edge.source].Cluster] + clustersY[graph.Vertexes[edge.target].Cluster]) / 2;
                }
            }
        }

        internal enum DrawModEnum
        {
            ROUND, RANDOM, TIME, WEIGHT, CLUSTERS
        }
    }
}
