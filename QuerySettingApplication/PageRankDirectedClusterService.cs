using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using MathNet.Numerics.LinearAlgebra;
using MoreLinq;
using QuerySettingApplication.Annotations;

namespace QuerySettingApplication
{
    public class PageRankDirectedClusterService<T> : IClusterService where T : Vertex, new()
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
        private int[] _cluster;

        private List<int>[] _vertexEdge;
        private List<int>[] _clusterVertexes;
        private List<int>[] _clusterEdge;
        private Matrix<float> P;
        private Vector<float> pi;

        private string _logPath = "pr.log";

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
            _clusterVertexes = new List<int>[_numV];
            _vertexEdge = new List<int>[_numV];
            _clusterEdge = new List<int>[_numV];

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

                if (_vertexEdge[edge.source] == null)
                    _vertexEdge[edge.source] = new List<int>();
                _vertexEdge[edge.source].Add(edge.target);
            }

            var G = Matrix<float>.Build.Dense(_numV, _numV, CalcGij);
            var t = Vector<float>.Build.Dense(_numV);
            t.At(0, 1);

            Vector<float> k;

            do
            {
                k = t;
                t = t*G;
            } while ((k - t).Norm(2) > 0.0001);

            pi = t;
            P = Matrix<float>.Build.Dense(_numV, _numV, (i, j) => pi.At(i) * G[i, j] - pi[i] * pi[j]);

            foreach (var edge in graph.Edges)
            {
                edge.weight = pi.At(edge.source) * G[edge.source, edge.target];
            }

            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        private float CalcGij(int i, int j)
        {
            const float alpha = (float) 0.85;

            var sum = _outDegree[i];
            var a = sum == 0 ? 1.0 : 0.0;
            var aij = 0.0;
            if (_vertexEdge[i] != null)
                aij = _vertexEdge[i].Count(e => e == j);
            var f = sum == 0 ? 0.0 : alpha*aij / sum;
            return (float) (f + (alpha * a + 1.0 - alpha) / _numV);
        }

        /*private double CalcGijMarkov(int i, int j)
        {
            var aij = 0.0;
            if (A[i] != null)
                aij = A[i].Contains(j) ? 1.0 : 0.0;

            if (A[i, j] == 0)
                return 0;

            var sum = 0.0;
            for (int k = 0; k < _numV; k++)
                sum += A[i, k];

            return 1 / sum;
        }*/

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

        public void FG()
        {
            _vertexMovePriotizer.Initialize(this);

            double deltaMax = 0;
            double oldMod = _modilarity;
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

            double deltaMax = 0;
            double oldMod = _modilarity;
            double k = 10 * Math.Log(_numV, 2);
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
            return _vertexEdge[v];
        }

        public List<int> GetIndClusters(int c)
        {
            return _clusterEdge[c];
        }

        internal double Weight(int source, int target)
        {
            var aij = 0.0;
            if (_vertexEdge[source] != null)
                aij = _vertexEdge[source].Count(v => v == target);

            return aij;
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
            return GetContainigCluster(i) == GetContainigCluster(j) ? 1 : 0;
        }

        internal double Weight(int source, List<int> targets)
        {
            return targets.Sum(target => Weight(source, target));
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

        internal void RecalcWeightOfClustering()
        {
            float result = _clusterVertexes.Where(v => v != null).Sum(vertexes => vertexes.Sum(v1 => vertexes.Sum(v2 => P[v1, v2])));

            _modilarity = result;

            using (var s = File.AppendText(_logPath))
            {
                s.WriteLine(_modilarity);
            }
        }

        public float DeltaWeightOfMerge(int C, int D)
        {
            var Cs = _clusterVertexes[C];
            var Ds = _clusterVertexes[D];

            var res = (from i in Cs from j in Ds select P[i, j]).Sum();
            res += (from i in Ds from j in Cs select P[i, j]).Sum();
            return res;
        }

        public float DeltaWeightOfMoving(int v, int D)
        {
            var C = GetContainigCluster(v);

            var vC = _clusterVertexes[C];
            var vD = _clusterVertexes[D];
            if (vD == null)
                return 0;
            var fvD = (float) 0.0;
            var fvC = (float) 0.0;

            foreach (var c in vC)
            {
                fvC += P[c, v];
                fvC += P[v, c];
            }

            foreach (var d in vD)
            {
                fvD += P[d, v];
                fvD += P[v, d];
            }

            return fvD - fvC;
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
}
