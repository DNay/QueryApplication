﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _currentEntityProp;
        private string _predicateTextProp;
        private int _numVertexesProp;
        private int _numEdgesProp;
        private int _maxVertCountProp;

        public MainWindow()
        {
            InitializeComponent();
            //ServiceSingletons.MainWindow = this;
            MaxVertCountProp = 600;
            CurrentEntityProp = "http://acm.rkbexplorer.com/id/1008169";
            PredicateTextProp = "akt:cites-publication-reference";
        } 

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                ServiceSingletons.QueryProcessor.StartProcess(PredicateTextProp, CurrentEntityProp, MaxVertCountProp);
                UpdateFields();
            });
        }

        public void OnQueryProcExited(object sender, EventArgs e)
        {
            NumVertexes.Content = ServiceSingletons.QueryProcessor.GetVertexesCount();
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Граф (.xml)|*.xml";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(CiteNet));
                    var fileStream = File.Create(saveDialog.FileName);
                    serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.CiteNet);
                    fileStream.Close();

                    var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.CiteNet);
                    var fileStreamJ = File.CreateText(saveDialog.FileName.Replace(".xml", ".json"));
                    fileStreamJ.Write(strJ);
                    fileStreamJ.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private bool OpenXml(string path)
        {
            var result = false;

            FileStream fileStream = File.OpenRead(path);
            try
            {
                var serializer = new XmlSerializer(typeof (CiteNet));

                var graph = serializer.Deserialize(fileStream) as CiteNet;
                ServiceSingletons.QueryProcessor.CiteNet = graph;
                ServiceSingletons.QueryProcessor.GraphAuthors = new AuthorsGraph();
                UpdateFields();

                result = true;
            }
            catch (Exception)
            {
                result = false;
            }
            finally
            {
                fileStream.Close();
            }

            if (!result)
            {
                fileStream = File.OpenRead(path);
                try
                {
                    var serializer = new XmlSerializer(typeof (AuthorsGraph));

                    var graph = serializer.Deserialize(fileStream) as AuthorsGraph;
                    ServiceSingletons.QueryProcessor.GraphAuthors = graph;
                    ServiceSingletons.QueryProcessor.CiteNet = new CiteNet();
                    fileStream.Close();
                    UpdateFields();

                    result = true;
                }
                catch (Exception)
                {
                    result = false;
                }

            }

            return result;
        }

        private void LoadFromFile(string fileName)
        {
            Task.Factory.StartNew(() =>
            {
                var gr = ServiceSingletons.QueryProcessor.CiteNet = new CiteNet();
                using (var file = File.OpenText(fileName))
                {
                    while (true)
                    {
                        var str = file.ReadLine();

                        if (string.IsNullOrEmpty(str))
                            break;

                        if (str.StartsWith("#"))
                            continue;

                        var res = str.Split(new[] {'\t', ' '}).ToList();

                        var outV = gr.AddVertex(res[0]);
                        var inV = gr.AddVertex(res[1]);

                        gr.AddEdge(new Edge(outV.Id, inV.Id), false);

                        NumVertexesProp = gr.NumVertexes;
                        NumEdgesProp = gr.Edges.Count;
                    }
                }
            });
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            //openDialog.Filter = "Граф (.xml)|*.xml";

            if (openDialog.ShowDialog() == true)
            {
                var isOpened = OpenXml(openDialog.FileName);

                if (!isOpened)
                    LoadFromFile(openDialog.FileName);
            }

            UpdateFields();
        }

        private void SaveAuth_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Граф (.xml)|*.xml";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(AuthorsGraph));
                    var fileStream = File.Create(saveDialog.FileName);
                    serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.GraphAuthors);
                    fileStream.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void LoadAuth_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.QueryProcessor.GetAuthors2();
            ServiceSingletons.QueryProcessor.CiteNet = new CiteNet();
            UpdateFields();
        }

        private void UpdateFields()
        {
            if (ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any())
            {
                NumVertexesProp = ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Count;
                NumEdgesProp = ServiceSingletons.QueryProcessor.GraphAuthors.Edges.Count;
            }
            else
            {
                NumVertexesProp = ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Count;
                NumEdgesProp = ServiceSingletons.QueryProcessor.CiteNet.Edges.Count;
            }


            RaisePropertyChanged("GraphDescription");
            RaisePropertyChanged("IsVisibleGraphDescriptionPrp");
            RaisePropertyChanged("IsClusteringEnabled");
        }

        private void Cluster_Click(object sender, RoutedEventArgs e)
        {
            var isAuthNet = ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any();
            
            var service = LinkRankMode.IsChecked.HasValue && LinkRankMode.IsChecked.Value
                ? ServiceSingletons.PageRankClusterService
                : ServiceSingletons.ClusterService;

            if (isAuthNet)
                service = LinkRankMode.IsChecked.HasValue && LinkRankMode.IsChecked.Value
                    ? ServiceSingletons.PageRankClusterAuthService
                    : ServiceSingletons.ClusterAuthService;

            IGraph graph = isAuthNet
                    ? (IGraph) ServiceSingletons.QueryProcessor.GraphAuthors
                    : ServiceSingletons.QueryProcessor.CiteNet;

            var clusterWindow = new ClusteringWindow(graph, service);
            clusterWindow.Closed += OnBack;
            clusterWindow.Show();
            this.Hide();
        }
        private void ClusterPageRank_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow(ServiceSingletons.QueryProcessor.CiteNet, ServiceSingletons.PageRankClusterService);
            clusterWindow.Show();
        }

        private void ClusterAuth_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow(ServiceSingletons.QueryProcessor.GraphAuthors, ServiceSingletons.ClusterAuthService);
            clusterWindow.Show();
        }

        private void ClusterAuthPageRank_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow(ServiceSingletons.QueryProcessor.GraphAuthors, ServiceSingletons.PageRankClusterAuthService);
            clusterWindow.Show();
        }

        public string CurrentEntityProp
        {
            get { return _currentEntityProp; }
            set
            {
                _currentEntityProp = value;
                RaisePropertyChanged("CurrentEntityProp");
            }
        }

        public int NumVertexesProp
        {
            get { return _numVertexesProp; }
            set
            {
                _numVertexesProp = value;
                RaisePropertyChanged("NumVertexesProp");
            }
        }

        public int NumEdgesProp
        {
            get { return _numEdgesProp; }
            set
            {
                _numEdgesProp = value;
                RaisePropertyChanged("NumEdgesProp");
            }
        }

        public bool IsClusteringEnabled
        {
            get { return ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Any() || ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any(); }
        }

        public int MaxVertCountProp
        {
            get { return _maxVertCountProp; }
            set
            {
                _maxVertCountProp = value;
                RaisePropertyChanged("MaxVertCountProp");
            }
        }

        public string PredicateTextProp
        {
            get { return _predicateTextProp; }
            set
            {
                _predicateTextProp = value;
                RaisePropertyChanged("PredicateTextProp");
            }
        }

        public bool IsClusteringAuthEnabled
        {
            get { return ServiceSingletons.QueryProcessor.GraphAuthors != null; }
        }

        private string _numVertexesAuthProp;
        public string NumVertexesAuthProp
        {
            get { return _numVertexesAuthProp; }
            set
            {
                _numVertexesAuthProp = value;
                RaisePropertyChanged("NumVertexesAuthProp");
            }
        }

        private string _numEdgesAuthProp;

        public string NumEdgesAuthProp
        {
            get { return _numEdgesAuthProp; }
            set
            {
                _numEdgesAuthProp = value;
                RaisePropertyChanged("NumEdgesAuthProp");
            }
        }

        public string GraphDescription
        {
            get
            {
                if (ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any())
                    return "Граф авторов";
                return ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Any() ? "Сеть цитирования" : "Граф не загружен";
            }
        }

        public bool IsVisibleGraphDescriptionPrp
        {
            get
            {
                return ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Any() ||
                       ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any();
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void LoadFromRep_Click(object sender, RoutedEventArgs e)
        {
            var win = new OnlineLoadWindow();
            win.Closed += OnBack;

            win.Show();
            this.Hide();
        }

        private void OnBack(object sender, EventArgs eventArgs)
        {
            this.Show();
        }
    }
}
