using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreLinq;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    public class QueryProcessor
    {
        private string _queryFileOut = "requests\\queryFileOut.rq";
        private string _queryFileIn = "requests\\queryFileIn.rq";
        private string _outFile = "outs\\out.json";
        private Graph _citeNet = new Graph();
        private Graph _graphAuthors = new Graph();
        private List<string> processedVertexes = new List<string>();
        private List<string> preparedVertexes = new List<string>();
        private Dictionary<string, string> _services = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _authors = new Dictionary<string, List<string>>();
        private int _maxVertexes = 600;

        public QueryProcessor()
        {
            _services.Add("acm", "http://acm.rkbexplorer.com/sparql");
        }

        public Graph CiteNet
        {
            get { return _citeNet; }
            set { _citeNet = value; }
        }

        public Graph GraphAuthors
        {
            get { return _graphAuthors; }
            set { _graphAuthors = value; }
        }

        public int GetVertexesCount()
        {
            return _citeNet.NumVertexes;
        }

        public bool IsAddNewVertexes { get { return GetVertexesCount() < _maxVertexes; } }

        public void StartProcess(string predicate, string initEntity, int maxVertCount)
        {
            preparedVertexes.Add(initEntity);
            _citeNet.AddVertex(initEntity);
            _maxVertexes = maxVertCount;
            do
            {
                var currentEntity = preparedVertexes.FirstOrDefault();

                if (currentEntity == null)
                    break;

                ServiceSingletons.MainWindow.CurrentEntityProp = currentEntity;

                ProcessSingleEntity(predicate, currentEntity);

                ServiceSingletons.MainWindow.NumVertexesProp = CiteNet.NumVertexes;
                ServiceSingletons.MainWindow.NumEdgesProp = CiteNet.Edges.Count;
            }
            while (true);
        }

        public void GetAuthors()
        {
            preparedVertexes.Clear();
            preparedVertexes.AddRange(CiteNet.Vertexes.Select(t => t.Texts.First()).Where(t => t != null));

            foreach (var currentEntity in preparedVertexes)
            {

                //ServiceSingletons.MainWindow.CurrentEntityProp = currentEntity;
                try
                {
                    ProcessAuthorQuery(currentEntity);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }

                ServiceSingletons.MainWindow.NumVertexesAuthProp = GraphAuthors.NumVertexes.ToString();
            }

            foreach (var edge in CiteNet.Edges)
            {
                if (!_authors.ContainsKey(CiteNet.Vertexes[edge.source].Texts.First()) || !_authors.ContainsKey(CiteNet.Vertexes[edge.target].Texts.First()))
                    continue;

                var sources = _authors[CiteNet.Vertexes[edge.source].Texts.First()];
                var targets = _authors[CiteNet.Vertexes[edge.target].Texts.First()];

                if (sources == null || targets == null)
                    continue;

                foreach (var source in sources)
                {
                    foreach (var target in targets)
                    {
                        var newEdge = new Edge(GraphAuthors.GetVertexId(source), GraphAuthors.GetVertexId(target));
                        GraphAuthors.AddEdge(newEdge);
                    }
                }
            }

            var authClusters = new Dictionary<string, Dictionary<int, int>>();

            foreach (var vertex in CiteNet.Vertexes)
            {
                var publ = vertex.Texts.First();

                if (!_authors.ContainsKey(publ))
                    continue;

                foreach (var author in _authors[publ])
                {
                    if (!authClusters.ContainsKey(author))
                        authClusters.Add(author, new Dictionary<int, int>());

                    if (!authClusters[author].ContainsKey(vertex.Cluster))
                        authClusters[author].Add(vertex.Cluster, 0);

                    authClusters[author][vertex.Cluster]++;
                }
            }

            foreach (var cluster in authClusters)
            {
                var cl = cluster.Value.Keys.First();
                var max = cluster.Value.Values.First();
                foreach (var pair in cluster.Value)
                {
                    if (max < pair.Value)
                    {
                        cl = pair.Key;
                        max = pair.Value;
                    }
                }

                GraphAuthors.GetVertex(cluster.Key).Cluster = cl;
            }
        }

        private void ProcessAuthorQuery(string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileOut, GetService(currentEntity), currentEntity, "akt:has-author");

            var jsonResult = ProcessAnyQuery(reqFile);

            var auths = jsonResult.Results.Bindings.Select(t => t.Entity).Select(t => t.Value).ToList();
            _authors.Add(currentEntity, new List<string>(auths));

            foreach (var auth in auths)
            {
                _graphAuthors.AddVertex(auth);
            }
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
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            } 
        }

        private void ProcessInQuery(string predicate, string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileIn, GetService(currentEntity), currentEntity, predicate);

            var jsonResult = ProcessAnyQuery(reqFile);

            if (jsonResult.Results.Bindings.Length <= 50)
            {
                foreach (var v in jsonResult.Results.Bindings
                                    .Select(t => t.Entity.Value)
                                    .Where(s => _citeNet.GetVertex(s) == null && IsAddNewVertexes))
                {
                    _citeNet.AddVertex(v);
                    preparedVertexes.Add(v);
                }
            }

            _citeNet.Edges.AddRange(jsonResult.Results.Bindings
                                                    .Where(t => _citeNet.GetVertex(t.Entity.Value) != null)
                                                    .Select(t => new Edge(_citeNet.GetVertexId(t.Entity.Value), _citeNet.GetVertexId(currentEntity)))
                                                    .Where(s => !_citeNet.Edges.Contains(s)));
        }

        private void ProcessOutQuery(string predicate, string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileOut, GetService(currentEntity), currentEntity, predicate);

            var jsonResult = ProcessAnyQuery(reqFile);

            if (jsonResult.Results.Bindings.Length <= 50)
            {
                foreach (var v in jsonResult.Results.Bindings
                                    .Select(t => t.Entity.Value)
                                    .Where(s => _citeNet.GetVertex(s) == null && IsAddNewVertexes))
                {
                    _citeNet.AddVertex(v);
                    preparedVertexes.Add(v);
                }
            }

            _citeNet.Edges.AddRange(jsonResult.Results.Bindings
                                                    .Where(t => _citeNet.GetVertex(t.Entity.Value) != null)
                                                    .Select(t => new Edge(_citeNet.GetVertexId(currentEntity), _citeNet.GetVertexId(t.Entity.Value)))
                                                    .Where(s => !_citeNet.Edges.Contains(s)));
        }

        private JSONResult ProcessAnyQuery(string reqFile)
        {
            var command = "jruby s-query --service=http://localhost:3030/sparql --query=\"..\\" + reqFile + "\" > \"..\\" +
                          _outFile + "\"";
            ServiceSingletons.JenaFusekiHelper.SendQuery(command);

            var file = File.ReadAllText(_outFile);
            var jsonResult = JsonConvert.DeserializeObject<JSONResult>(file);

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
