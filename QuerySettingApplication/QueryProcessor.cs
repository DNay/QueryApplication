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
        private string _queryFileInfo = "requests\\queryFileAll.rq";
        private string _outFile = "outs\\out.json";
        private CiteNet _citeNet = new CiteNet();
        private AuthorsGraph _graphAuthors = new AuthorsGraph();
        private List<string> processedVertexes = new List<string>();
        private List<string> preparedVertexes = new List<string>();
        private Dictionary<string, string> _services = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _authors = new Dictionary<string, List<string>>();
        private int _maxVertexes = 600;

        public QueryProcessor()
        {
            _services.Add("acm", "http://acm.rkbexplorer.com/sparql");
            _services.Add("dblp", "http://dblp.rkbexplorer.com/sparql");
        }

        public CiteNet CiteNet
        {
            get { return _citeNet; }
            set { _citeNet = value; }
        }

        public AuthorsGraph GraphAuthors
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
            preparedVertexes.AddRange(CiteNet.Vertexes.Select(t => t.Name).Where(t => t != null));

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
                if (!_authors.ContainsKey(CiteNet.Vertexes[edge.source].Name) || !_authors.ContainsKey(CiteNet.Vertexes[edge.target].Name))
                    continue;

                var sources = _authors[CiteNet.Vertexes[edge.source].Name];
                var targets = _authors[CiteNet.Vertexes[edge.target].Name];

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
        }

        public void GetAuthors2()
        {
            foreach (VertexPublication vert in CiteNet.Vertexes)
            {
                if (!_authors.ContainsKey(vert.Name))
                    _authors.Add(vert.Name, vert.Authors);

                if (vert.Authors != null)
                    foreach (var auth in vert.Authors)
                    {
                        if (GraphAuthors.Vertexes.All(t => t.Name != auth))
                            GraphAuthors.AddVertex(auth);
                    }

                ServiceSingletons.MainWindow.NumVertexesAuthProp = GraphAuthors.NumVertexes.ToString();
            }

            foreach (var edge in CiteNet.Edges)
            {
                if (!_authors.ContainsKey(CiteNet.Vertexes[edge.source].Name) || !_authors.ContainsKey(CiteNet.Vertexes[edge.target].Name))
                    continue;

                var sources = _authors[CiteNet.Vertexes[edge.source].Name];
                var targets = _authors[CiteNet.Vertexes[edge.target].Name];

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

            foreach (var v in GraphAuthors.Vertexes)
            {
                v.Infos = ProcessAuthorInfoQuery(v.Name);
            }

            ServiceSingletons.MainWindow.NumEdgesAuthProp = GraphAuthors.Edges.Count.ToString();
        }

        private void ProcessAuthorQuery(string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileOut, GetService(currentEntity), currentEntity, "akt:has-author");

            var jsonResult = ProcessAnyQuery<JSONResult>(reqFile);

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
                ProcessInfoQuery(currentEntity);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            } 
        }

        private List<RdfInfo> ProcessAuthorInfoQuery(string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileInfo, GetService(currentEntity), currentEntity, string.Empty);

            var jsonResult = ProcessAnyQuery<JSONResultInfo>(reqFile);
            return jsonResult.Results.Bindings.Select(t => new RdfInfo(t.P.Value, t.S.Value)).Distinct().ToList();
        }

        private void ProcessInfoQuery(string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileInfo, GetService(currentEntity), currentEntity, string.Empty);

            var jsonResult = ProcessAnyQuery<JSONResultInfo>(reqFile);
            var infos = jsonResult.Results.Bindings.Select(t => new RdfInfo(t.P.Value, t.S.Value)).Distinct().ToList();

            var authors = infos.Where(t => t.Predicate.Contains("has-author")).Select(t => t.Subject).Distinct().ToList();
            var dates = infos.Where(t => t.Predicate.Contains("has-date")).Select(t =>
            {
                var text = t.Subject;
                text = text.Replace("http://www.aktors.org/ontology/date#",
                    string.Empty);
                return DateTime.Parse(text);
            })
                                                                        .Distinct().ToList();

            var curVert = CiteNet.GetVertex(currentEntity);

            curVert.Infos = infos;
            curVert.Date = dates.Max();
            curVert.Authors = authors;
        }

        private void ProcessInQuery(string predicate, string currentEntity)
        {
            var reqFile = GenerateRequest(_queryFileIn, GetService(currentEntity), currentEntity, predicate);

            var jsonResult = ProcessAnyQuery<JSONResult>(reqFile);

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

            var jsonResult = ProcessAnyQuery<JSONResult>(reqFile);

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

        private T ProcessAnyQuery<T>(string reqFile)
        {
            var command = "jruby s-query --service=http://localhost:3030/sparql --query=\"..\\" + reqFile + "\" > \"..\\" +
                          _outFile + "\"";
            ServiceSingletons.JenaFusekiHelper.SendQuery(command);

            var file = File.ReadAllText(_outFile);
            var jsonResult = JsonConvert.DeserializeObject<T>(file);

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
