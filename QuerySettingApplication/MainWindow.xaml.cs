using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _numVertexesProp;
        private int _numEdgesProp;

        public MainWindow()
        {
            InitializeComponent();
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
                    if (ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Any())
                    {
                        var serializer = new XmlSerializer(typeof (CiteNet));
                        var fileStream = File.Create(saveDialog.FileName);
                        serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.CiteNet);
                        fileStream.Close();
                    }
                    else if (ServiceSingletons.QueryProcessor.GraphAuthors.Vertexes.Any())
                    {
                        var serializer = new XmlSerializer(typeof(AuthorsGraph));
                        var fileStream = File.Create(saveDialog.FileName);
                        serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.GraphAuthors);
                        fileStream.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private bool OpenXml(string path)
        {
            bool result;

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
                UpdateFields();
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
            Hide();
        }

        private void OnBack(object sender, EventArgs eventArgs)
        {
            Show();
        }
    }
}
