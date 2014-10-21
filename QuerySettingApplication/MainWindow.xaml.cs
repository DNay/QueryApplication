using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
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
            ServiceSingletons.QueryProcessor.StartProcess(PredicateTextBox.Text, EntityTextBox.Text);
            UpdateFields();
        }

        private void Fuseki_Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.JenaFusekiHelper.StartServer();
        }

        public void OnQueryProcExited(object sender, EventArgs e)
        {
            NumVertexes.Content = ServiceSingletons.QueryProcessor.GetVertexesCount();
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Graph));
            var fileStream = File.Create("Graph.xml");
            serializer.Serialize(fileStream, ServiceSingletons.QueryProcessor.Graph);
            fileStream.Close();

            var strJ = JsonConvert.SerializeObject(ServiceSingletons.QueryProcessor.Graph);
            var fileStreamJ = File.CreateText("Graph.json");
            fileStreamJ.Write(strJ);
            fileStreamJ.Close();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Graph));
            FileStream fileStream = File.OpenRead("Graph.xml");
            ServiceSingletons.QueryProcessor.Graph = serializer.Deserialize(fileStream) as Graph;
            fileStream.Close();
            UpdateFields();
        }

        private void UpdateFields()
        {
            NumVertexes.Content = ServiceSingletons.QueryProcessor.Graph.Vertexes.Count;
            NumEdges.Content = ServiceSingletons.QueryProcessor.Graph.Edges.Count;
        }

        //BrowserWindow br = new BrowserWindow();
    }
}
