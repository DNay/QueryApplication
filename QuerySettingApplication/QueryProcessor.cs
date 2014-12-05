using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    public class QueryProcessor
    {
        private string _queryFileOut = "requests\\queryFileOut.rq";
        private string _queryFileIn = "requests\\queryFileIn.rq";
        private string _outFile = "outs\\out.json";
        private Graph _graph = new Graph();
        private List<string> processedVertexes = new List<string>();
        private List<string> preparedVertexes = new List<string>();
        private Dictionary<string, string> _services = new Dictionary<string, string>();
        private int _maxVertexes = 600;

        public QueryProcessor()
        {
            _services.Add("acm", "http://acm.rkbexplorer.com/sparql");
        }

        public Graph Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        public int GetVertexesCount()
        {
            return _graph.NumVertexes;
        }

        public bool IsAddNewVertexes { get { return GetVertexesCount() < _maxVertexes; } }

        public void StartProcess(string predicate, string initEntity, int maxVertCount)
        {
            preparedVertexes.Add(initEntity);
            _graph.AddVertex(initEntity);
            _maxVertexes = maxVertCount;
            do
            {
                var currentEntity = preparedVertexes.FirstOrDefault();

                if (currentEntity == null)
                    return;

                ProcessSingleEntity(predicate, currentEntity);
            }
            while (true);
        }

        private void ProcessSingleEntity(string predicate, string currentEntity)
        {
            processedVertexes.Add(currentEntity);
            preparedVertexes.Remove(currentEntity);
            try
            {
                ProcessInQuery(predicate, currentEntity);
                ProcessOutQuery(predicate, currentEntity);
            }
            catch (Exception)
            {

            } 
        }

        private void ProcessInQuery(string predicate, string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileIn, GetService(currentEntity), currentEntity, predicate);

            var jsonResult = ProcessAnyQuery(reqFile);

            _graph.Edges.AddRange(jsonResult.Results.Bindings
                                                    .Where(t => _graph.GetVertex(t.Entity.Value) != null)
                                                    .Select(t => new Edge(_graph.GetVertexId(t.Entity.Value), _graph.GetVertexId(currentEntity)))
                                                    .Where(s => !_graph.Edges.Contains(s)));
        }

        private void ProcessOutQuery(string predicate, string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileOut, GetService(currentEntity), currentEntity, predicate);

            var jsonResult = ProcessAnyQuery(reqFile);

            _graph.Edges.AddRange(jsonResult.Results.Bindings
                                                    .Where(t => _graph.GetVertex(t.Entity.Value) != null)
                                                    .Select(t => new Edge(_graph.GetVertexId(currentEntity), _graph.GetVertexId(t.Entity.Value)))
                                                    .Where(s => !_graph.Edges.Contains(s)));
        }

        private JSONResult ProcessAnyQuery(string reqFile)
        {
            var command = "jruby s-query --service=http://localhost:3030/sparql --query=\"..\\" + reqFile + "\" > \"..\\" +
                          _outFile + "\"";
            ServiceSingletons.JenaFusekiHelper.SendQuery(command);

            var file = File.ReadAllText(_outFile);
            var jsonResult = JsonConvert.DeserializeObject<JSONResult>(file);

            if (jsonResult.Results.Bindings.Length >= 50)
                return jsonResult;

            foreach (var v in jsonResult.Results.Bindings
                                                .Select(t => t.Entity.Value)
                                                .Where(s => _graph.GetVertex(s) == null && IsAddNewVertexes))
            {
                _graph.AddVertex(v);
            }
            preparedVertexes.InsertRange(0, jsonResult.Results.Bindings
                                                        .Select(t => t.Entity.Value)
                                                        .Where(s => !preparedVertexes.Contains(s) && !processedVertexes.Contains(s) && IsAddNewVertexes));

            return jsonResult;
        }

        private string GetService(string currentEntity)
        {
            return _services.FirstOrDefault(t => currentEntity.Contains(t.Key)).Value;
        }

        private string GenerateRequest(string fileName, string service, string entity, string predicate)
        {
            var queryText = File.ReadAllText(fileName + ".template");
            var newQuery = queryText.Replace("$TEMPLATE_SERVICE$", service).Replace("$TEMPLATE_ENTITY$", entity).Replace("$PREDICATE$", predicate);
            File.WriteAllText(fileName, newQuery);
            return fileName;
        }
    }
}
