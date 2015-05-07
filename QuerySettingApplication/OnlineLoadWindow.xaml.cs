using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using QuerySettingApplication.Annotations;

namespace QuerySettingApplication
{
    /// <summary>
    /// Interaction logic for OnlineLoadWindow.xaml
    /// </summary>
    public partial class OnlineLoadWindow : Window, INotifyPropertyChanged
    {
        private string _currentEntityProp;
        private string _predicateTextProp = "";
        private int _numVertexesProp;
        private int _numEdgesProp;
        private int _maxVertCountProp;

        public OnlineLoadWindow()
        {
            InitializeComponent();
            ServiceSingletons.MainWindow = this;
            MaxVertCountProp = 500;
            CurrentEntityProp = "http://acm.rkbexplorer.com/id/806991";
            PredicateTextProp = "akt:cites-publication-reference";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Fuseki_Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.JenaFusekiHelper.StartServer();
            StartProcess.IsEnabled = true;
            ServerStatus.Content = "Fuseki server is ON";
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceSingletons.QueryProcessor.GraphAuthors = new AuthorsGraph();
            Task.Factory.StartNew(() =>
            {
                ServiceSingletons.QueryProcessor.StartProcess(PredicateTextProp, CurrentEntityProp, MaxVertCountProp);
                UpdateFields();
            });
        }

        public string PredicateTextProp
        {
            get { return _predicateTextProp; }
            set
            {
                _predicateTextProp = value;
                OnPropertyChanged("PredicateTextProp");
            }
        }

        public string CurrentEntityProp
        {
            get { return _currentEntityProp; }
            set
            {
                _currentEntityProp = value;
                OnPropertyChanged("CurrentEntityProp");
            }
        }

        public int NumVertexesProp
        {
            get { return _numVertexesProp; }
            set
            {
                _numVertexesProp = value;
                OnPropertyChanged("NumVertexesProp");
            }
        }

        public int NumEdgesProp
        {
            get { return _numEdgesProp; }
            set
            {
                _numEdgesProp = value;
                OnPropertyChanged("NumEdgesProp");
            }
        }

        public int MaxVertCountProp
        {
            get { return _maxVertCountProp; }
            set
            {
                _maxVertCountProp = value;
                OnPropertyChanged("MaxVertCountProp");
            }
        }

        private void UpdateFields()
        {
            NumVertexesProp = ServiceSingletons.QueryProcessor.CiteNet.Vertexes.Count;
            NumEdgesProp = ServiceSingletons.QueryProcessor.CiteNet.Edges.Count;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
