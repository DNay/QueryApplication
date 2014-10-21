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
    }
}
