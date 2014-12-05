using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ServiceSingletons.MainWindow = this;
        } 

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            int max;
            if (!int.TryParse(MaxVertCount.Text, out max))
                return;
            ServiceSingletons.QueryProcessor.StartProcess(PredicateTextBox.Text, EntityTextBox.Text, max);
            UpdateFields();
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
                    serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.Graph);
                    fileStream.Close();

                    var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.Graph);
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
                    ServiceSingletons.QueryProcessor.Graph = graph;
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

        private void UpdateFields()
        {
            NumVertexes.Content = ServiceSingletons.QueryProcessor.Graph.Vertexes.Count;
            NumEdges.Content = ServiceSingletons.QueryProcessor.Graph.Edges.Count;
            ClusterButton.IsEnabled = true;
        }

        private void Cluster_Click(object sender, RoutedEventArgs e)
        {
            var clusterWindow = new ClusteringWindow();
            clusterWindow.Show();
        }
    }
}
