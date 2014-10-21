using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    public class QueryProcessor
    {
        private string _queryFile = "requests\\queryFile.rq";
        private string _outFile = "outs\\out.json";
        private Graph _graph = new Graph();
        private List<string> processedVertexes = new List<string>();
        private List<string> preparedVertexes = new List<string>();

        public Graph Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        public int GetVertexesCount()
        {
            return _graph.NumVertexes;
        }

        public void StartProcess(string service, string initEntity)
        {
            preparedVertexes.Add(initEntity);
            _graph.AddVertex(initEntity);

            do
            {
                var currentEntity = preparedVertexes.FirstOrDefault();

                if (currentEntity == null || Graph.Edges.Count > 100)
                    return;

                var reqFile = GenerateRequest(service, currentEntity);

                var command = "jruby s-query --service=http://localhost:3030/sparql --query=\"..\\" + reqFile + "\" > \"..\\" + _outFile + "\"";
                ServiceSingletons.JenaFusekiHelper.SendQuery(command);

                var file = File.ReadAllText(_outFile);
                var jsonResult = JsonConvert.DeserializeObject<JSONResult>(file);

                processedVertexes.Add(currentEntity);
                preparedVertexes.Remove(currentEntity);

                foreach (var v in jsonResult.Results.Bindings.Select(t => t.Entity.Value).Where(s => _graph.GetVertex(s) == null))
                {
                    _graph.AddVertex(v);
                }

                _graph.Edges.AddRange(jsonResult.Results.Bindings.Select(t => new Edge(_graph.GetVertexId(currentEntity), _graph.GetVertexId(t.Entity.Value))).Where(s => !_graph.Edges.Contains(s)));
                preparedVertexes.AddRange(jsonResult.Results.Bindings.Select(t => t.Entity.Value).Where(s => !preparedVertexes.Contains(s) && !processedVertexes.Contains(s)));
            }
            while (true);
        }

        private string GenerateRequest(string service, string entity)
        {
            var queryText = File.ReadAllText(_queryFile + ".template");
            var newQuery = queryText.Replace("$TEMPLATE_SERVICE$", service).Replace("$TEMPLATE_ENTITY$", entity);
            File.WriteAllText(_queryFile, newQuery);
            return _queryFile;
        }
    }
}
