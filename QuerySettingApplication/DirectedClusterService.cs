using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreLinq;

namespace QuerySettingApplication
{
    public class DirectedClusterService<T> : IClusterService where T : Vertex, new()
    {
        //public Clustering Clustering { get; set; }

        //private List<EdgeWrapper> Edges = new List<EdgeWrapper>();

        private MergePriotizer<T> _mergePriotizer = new MergePriotizer<T>();
        private VertexMovePriotizer<T> _vertexMovePriotizer = new VertexMovePriotizer<T>();
        private bool IsAccurate = false;

        private int _numV;
        private int _numE;
        private float _modilarity;
        private float[] _inDegree;
        private float[] _outDegree;
        private int[,] _matr;
        private int[] _cluster;

        private List<int>[] _vertexEdge;
        private List<int>[] _clusterVertexes;
        private List<int>[] _clusterEdge;
        private List<int>[] A;

        private string _logPath = "cmn.log";

        public void SetGraph(IGraph graph)
        {
            Graph = graph;
            SetGraph(graph as Graph<T>);
        }

        public void SetGraph(Graph<T> graph)
        {
            if (File.Exists(_logPath))
                File.Delete(_logPath);
            using (var f = File.Create(_logPath))
            { }

            _numV = graph.NumVertexes;
            _numE = graph.Edges.Count;
            _inDegree = new float[_numV];
            _outDegree = new float[_numV];
            _cluster = new int[_numV];
            _matr = new int[_numV, _numV];
            _clusterVertexes = new List<int>[_numV];
            _vertexEdge = new List<int>[_numV];
            _clusterEdge = new List<int>[_numV];
            A = new List<int>[_numV];

            foreach (var vertex in graph.Vertexes)
            {
                _cluster[vertex.Id] = vertex.Cluster;
                if (_clusterVertexes[vertex.Cluster] == null)
                {
                    _clusterVertexes[vertex.Cluster] = new List<int>();
                    _clusterEdge[vertex.Cluster] = new List<int>();
                }
                _clusterVertexes[vertex.Cluster].Add(vertex.Id);
            }

            foreach (var edge in graph.Edges)
            {
                _inDegree[edge.target]++;
                _outDegree[edge.source]++;

                _matr[edge.source, edge.target] = 1;

                if (A[edge.source] == null)
                    A[edge.source] = new List<int>();
                A[edge.source].Add(edge.target);

                if (_vertexEdge[edge.source] == null)
                    _vertexEdge[edge.source] = new List<int>();
                _vertexEdge[edge.source].Add(edge.target);
            }

            foreach (var edge in graph.Edges)
            {
                edge.weight = (_matr[edge.source, edge.target] - (_outDegree[edge.source] * _inDegree[edge.target]) / EdgeCount())/100;
            }

            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        public void Initialize()
        {
            for (int i = 0; i < _numV; i++)
            {
                _clusterVertexes[i] = new List<int>() { i };
                _cluster[i] = i;
                _clusterEdge[i] = new List<int>();
            }

            for (var i = 0; i < _vertexEdge.Length; i++)
            {
                if (_vertexEdge[i] == null)
                    continue;

                foreach (var v in _vertexEdge[i])
                {
                    var t = GetContainigCluster(v);
                    var s = i;

                    if (!_clusterEdge[s].Contains(t))
                        _clusterEdge[s].Add(t);

                    if (!_clusterEdge[t].Contains(s))
                        _clusterEdge[t].Add(s);
                }
            }

            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        public void Renumber()
        {
            var l = _clusterVertexes.Where(v => v != null && v.Any()).ToList();
            for (int i = 0; i < _numV; i++)
            {
                if (i < l.Count)
                {
                    _clusterVertexes[i] = l[i];
                    foreach (var v in _clusterVertexes[i])
                    {
                        _cluster[v] = i;
                    }
                }
                else
                    _clusterVertexes[i] = null;
            }

            l = _clusterEdge.Where(e => e != null && e.Any()).ToList();
            for (int i = 0; i < _numV; i++)
            {
                if (i < l.Count)
                    _clusterEdge[i] = l[i];
                else
                    _clusterEdge[i] = null;
            }
        }

        public int GetContainigCluster(int id)
        {
            return _cluster[id];
        }

        public int NumClusters()
        {
            return _clusterVertexes.Count(t => t != null);
        }

        public void SSG()
        {
            _mergePriotizer.Initialize(this);

            double deltaMax = 0;

            do
            {
                int C;
                int D;

                deltaMax = _mergePriotizer.GetPrioritizedPair(out C, out D);
                if (deltaMax < 0)
                    break;

                Merge(C, D);

                ServiceSingletons.ClusterWindow.SetModularity(_modilarity);

            } while (true);

            Renumber();
        }

        public void CG()
        {
            _vertexMovePriotizer.Initialize(this);

            double deltaMax = 0;
            double oldMod = _modilarity;
            int num = 0;
            do
            {
                int V;
                int D;

                deltaMax = _vertexMovePriotizer.GetPrioritizedPair(out V, out D);
                if (deltaMax < 0)
                    break;

                Move(V, D);

                ServiceSingletons.ClusterWindow.SetModularity(_modilarity);
                var delta = _modilarity - oldMod;
                oldMod = _modilarity;
                num++;

            } while (true);

            Renumber();
        }


        internal int Weight(int source, int target)
        {
            return _matr[source, target];
        }

        internal double InDegree(int vertex)
        {
            return _inDegree[vertex];
        }

        internal double OutDegree(int vertex)
        {
            return _outDegree[vertex];
        }

        internal int IsInSameCluster(int i, int j)
        {
            return _cluster[i] == _cluster[j] ? 1 : 0;
        }

        internal int Weight(int source, List<int> targets)
        {
            return targets.Sum(target => _matr[source, target]);
        }

        public int NumVertexes()
        {
            return _numV;
        }

        internal float EdgeCount()
        {
            return _numE;
        }

        public float WeightOfClustering()
        {
            if (_modilarity == 0)
                RecalcWeightOfClustering();

            return _modilarity;
        }

        public void MSG(float mergeFactor)
        {
            _mergePriotizer.Initialize(this);
            do
            {
                var l = mergeFactor * _mergePriotizer.NumPozitivePairs();
                if (l == 0)
                    break;

                var pairs = _mergePriotizer.GetTopPrioritizedPairs((int)Math.Ceiling(l));
                var merged = new bool[_numV]; // all false => unmerged

                foreach (var pair in pairs)
                {
                    var c = pair.Key;
                    var d = pair.Value;
                    if (!merged[c] && !merged[d])
                    {
                        Merge(c, d);
                        merged[c] = merged[d] = true;
                        ServiceSingletons.ClusterWindow.SetModularity(_modilarity);
                    }
                }


            } while (true);

            Renumber();
        }

        public void FG()
        {
            _vertexMovePriotizer.Initialize(this);

            float deltaMax = 0;
            float oldMod = _modilarity;
            do
            {
                for (int i = 0; i < _numV; i++)
                {
                    int D;
                    deltaMax = _vertexMovePriotizer.GetBestCluster(i, out D);

                    if (deltaMax > 0)
                    {
                        Move(i, D);
                        var delta = _modilarity - oldMod;
                        oldMod = _modilarity;
                        ServiceSingletons.ClusterWindow.SetModularity(_modilarity);
                    }
                }

            } while (_vertexMovePriotizer.Pairs.Any());

            Renumber();
        }

        public void AKL()
        {
            _vertexMovePriotizer.Initialize(this);

            float deltaMax = 0;
            float oldMod = _modilarity;
            float k = (float) (10 * Math.Log(_numV, 2));
            do
            {
                var moved = new bool[_numV];

                for (int i = 0; i < k; i++)
                {
                    int V;
                    int D;

                    deltaMax = _vertexMovePriotizer.GetPrioritizedPair(moved, out V, out D);
                    moved[V] = true;
                    if (deltaMax > 0)
                    {
                        Move(V, D);
                        var delta = _modilarity - oldMod;
                        oldMod = _modilarity;
                        ServiceSingletons.ClusterWindow.SetModularity(_modilarity);
                    }
                }

            } while (_vertexMovePriotizer.Pairs.Any());

            Renumber();
        }

        public IGraph Graph { get; private set; }
        public List<int> GetIndVertexes(int v)
        {
            return A[v];
        }

        public List<int> GetIndClusters(int c)
        {
            return _clusterEdge[c];
        }

        internal void RecalcWeightOfClustering()
        {
            float result = 0;
            for (int i = 0; i < _numV; i++)
            {
                for (int j = 0; j < _numV; j++)
                {
                    result += (_matr[i, j] - (_outDegree[i] * _inDegree[j]) / EdgeCount()) * IsInSameCluster(i, j);
                }
            }

            _modilarity = result / EdgeCount();

            using (var s = File.AppendText(_logPath))
            {
                s.WriteLine(_modilarity);
            }
        }

        public float DeltaWeightOfMerge(int C, int D)
        {
            float result = 0;

            var outs = _clusterVertexes[C];
            var ins = _clusterVertexes[D];

            foreach (var i in ins)
            {
                foreach (var j in outs)
                {
                    result += (_matr[i, j] + _matr[j, i] - (_outDegree[i] * _inDegree[j] + _outDegree[j] * _inDegree[i]) / EdgeCount());
                }
            }

            return result / EdgeCount();
        }

        public float DeltaWeightOfMoving(int v, int D)
        {
            var C = _cluster[v];

            float result = 0;

            var vC = _clusterVertexes[C];
            var vD = _clusterVertexes[D];
            if (vD == null)
                return 0;
            var fvD = (float)0.0;
            var fvC = (float)0.0;

            foreach (var c in vC)
            {
                fvC += _matr[c, v];
                fvC += _matr[v, c];
            }

            foreach (var d in vD)
            {
                fvD += _matr[d, v];
                fvD += _matr[v, d];
            }

            result += fvD - fvC;

            switch (0)
            {
                case 0:
                    foreach (var i in vD)
                    {
                        result -= (_outDegree[v] * _inDegree[i] + _outDegree[i] * _inDegree[v]) / EdgeCount();
                    }

                    foreach (var j in vC)
                    {
                        result += (_outDegree[j] * _inDegree[v] + _outDegree[v] * _inDegree[j]) / EdgeCount();
                    }
                    break;
                case 1:
                    foreach (var i in vD)
                    {
                        result -= (_inDegree[v] * _outDegree[i]) / EdgeCount();
                    }

                    foreach (var j in vC)
                    {
                        result += (_inDegree[j] * _outDegree[v]) / EdgeCount();
                    }
                    break;
                case 2:
                    foreach (var i in vD)
                    {
                        result -= (_outDegree[v] * _inDegree[i]) / EdgeCount();
                    }

                    foreach (var j in vC)
                    {
                        result += (_inDegree[j] * _outDegree[v]) / EdgeCount();
                    }
                    break;
                case 3:
                    foreach (var i in vD)
                    {
                        result -= (_inDegree[v] * _outDegree[i]) / EdgeCount();
                    }

                    foreach (var j in vC)
                    {
                        result += (_outDegree[j] * _inDegree[v]) / EdgeCount();
                    }
                    break;
            }


            return result / EdgeCount();
        }

        internal void Move(int v, int D)
        {
            _cluster[v] = D;

            var C = GetContainigCluster(v);
            _clusterVertexes[C].Remove(v);
            if (!_clusterVertexes[C].Any())
                _clusterVertexes[C] = null;

            _clusterVertexes[D].Add(v);

            RecalcWeightOfClustering();
            _vertexMovePriotizer.OnMove(v, C, D, this);
        }

        internal void Merge(int C, int D)
        {
            foreach (var d in _clusterVertexes[D])
            {
                _cluster[d] = C;
            }

            _clusterVertexes[C].AddRange(_clusterVertexes[D]);
            _clusterVertexes[C] = _clusterVertexes[C].Distinct().ToList();
            _clusterVertexes[D] = null;

            _clusterEdge[C].AddRange(_clusterEdge[D]);
            _clusterEdge[C] = _clusterEdge[C].Distinct().ToList();
            _clusterEdge.ForEach(c => c.Remove(D));

            RecalcWeightOfClustering();

            _mergePriotizer.OnMerge(C, D, this);
        }
    }

    public interface IClusterService
    {
        int NumVertexes();
        float DeltaWeightOfMerge(int cl1, int cl2);
        float DeltaWeightOfMoving(int i, int cluster);
        void SetGraph(IGraph graph);

        void Initialize();
        void CG();
        void SSG();
        int GetContainigCluster(int id);
        int NumClusters();
        void Renumber();

        float WeightOfClustering();
        void MSG(float mergeFactor);
        void FG();
        void AKL();

        IGraph Graph { get; }
        List<int> GetIndVertexes(int v);
        List<int> GetIndClusters(int c);
    }
}
