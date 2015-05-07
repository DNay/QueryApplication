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

        public static OnlineLoadWindow MainWindow { get; set; }

        private static IClusterService _clusterServicePR = new PageRankDirectedClusterService<VertexPublication>();

        public static IClusterService PageRankClusterService
        {
            get { return _clusterServicePR; }
        }

        private static IClusterService _clusterAuthServicePR = new PageRankDirectedClusterService<Vertex>();

        public static IClusterService PageRankClusterAuthService
        {
            get { return _clusterAuthServicePR; }
        }

        private static IClusterService _clusterService = new DirectedClusterService<VertexPublication>();

        public static IClusterService ClusterService
        {
            get { return _clusterService; }
        }

        private static IClusterService _clusterAuthService = new DirectedClusterService<Vertex>();

        public static IClusterService ClusterAuthService
        {
            get { return _clusterAuthService; }
        }

        public static ClusteringWindow ClusterWindow { get; set; }
    }
}
