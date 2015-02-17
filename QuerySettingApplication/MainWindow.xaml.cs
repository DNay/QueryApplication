using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
            ServiceSingletons.MainWindow = this;
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

        private void Fuseki_Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.JenaFusekiHelper.StartServer();
            StartProcess.IsEnabled = true;
            ServerStatus.Content = "Fuseki server is ON";
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
                    var serializer = new XmlSerializer(typeof(Graph));
                    var fileStream = File.Create(saveDialog.FileName);
                    serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.CiteNet);
                    fileStream.Close();

                    var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.CiteNet);
                    var fileStreamJ = File.CreateText(saveDialog.FileName.Replace(".xml", ".json"));
                    fileStreamJ.Write(strJ);
                    fileStreamJ.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Граф (.xml)|*.xml";

            if (openDialog.ShowDialog() == true)
            {
                var serializer = new XmlSerializer(typeof (Graph));
                var fileStream = File.OpenRead(openDialog.FileName);

                try
                {
                    var graph = serializer.Deserialize(fileStream) as Graph;
                    ServiceSingletons.QueryProcessor.CiteNet = graph;
                    fileStream.Close();
                    UpdateFields();

                    //var strJ = File.ReadAllText(openDialog.FileName.Replace(".xml", ".json"));
                    //var graph = JsonConvert.DeserializeObject<Graph>(strJ);
                    //ServiceSingletons.QueryProcessor.Graph = graph;
                }

                catch (Exception)
                {
                    MessageBox.Show("Не удалось открыть файл");
                }

            }
        }

        private void SaveAuth_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Граф (.xml)|*.xml";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(Graph));
                    var fileStream = File.Create(saveDialog.FileName);
                    serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.GraphAuthors);
                    fileStream.Close();

                    var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.GraphAuthors);
                    var fileStreamJ = File.CreateText(saveDialog.FileName.Replace(".xml", ".json"));
                    fileStreamJ.Write(strJ);
                    fileStreamJ.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void LoadAuth_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Граф (.xml)|*.xml";

            if (openDialog.ShowDialog() == true)
            {
                var serializer = new XmlSerializer(typeof (Graph));
                var fileStream = File.OpenRead(openDialog.FileName);

                try
                {
                    var graph = serializer.Deserialize(fileStream) as Graph;
                    ServiceSingletons.QueryProcessor.GraphAuthors = graph;
                    fileStream.Close();
                    UpdateFields();

                    //var strJ = File.ReadAllText(openDialog.FileName.Replace(".xml", ".json"));
                    //var graph = JsonConvert.DeserializeObject<Graph>(strJ);
                    //ServiceSingletons.QueryProcessor.Graph = graph;
                }

                catch (Exception)
                {
                    MessageBox.Show("Не удалось открыть файл");
                }

            }
        }

        private void Authors_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.QueryProcessor.GetAuthors();
        }

        private void UpdateFields()
        {
            NumVertexesProp = ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Count;
            NumEdgesProp = ServiceSingletons.QueryProcessor.CiteNet.Edges.Count;
            RaisePropertyChanged("IsClusteringEnabled");
        }

        private void Cluster_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow(ServiceSingletons.QueryProcessor.CiteNet);
            clusterWindow.Show();
        }

        private void ClusterAuth_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow(ServiceSingletons.QueryProcessor.GraphAuthors);
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
            get { return ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Count > 0; }
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

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
