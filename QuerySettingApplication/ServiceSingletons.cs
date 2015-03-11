namespace QuerySettingApplication
{
    public class ServiceSingletons
    {
        private static QueryProcessor _queryProcessor = new QueryProcessor();

        public static QueryProcessor QueryProcessor 
        {
            get { return _queryProcessor; }
        }

        private static JenaFusekiHelper _jenaFusekiHelper = new JenaFusekiHelper();

        public static JenaFusekiHelper JenaFusekiHelper
        {
            get { return _jenaFusekiHelper; }
        }

        public static MainWindow MainWindow { get; set; }

        private static IClusterService _clusterService = new PageRankDirectedClusterService<VertexPublication>();//ClusterService();

        public static IClusterService ClusterService
        {
            get { return _clusterService; }
        }

        private static IClusterService _clusterAuthService = new PageRankDirectedClusterService<Vertex>();//ClusterService();

        public static IClusterService ClusterAuthService
        {
            get { return _clusterAuthService; }
        }

        public static ClusteringWindow ClusterWindow { get; set; }
    }
}
