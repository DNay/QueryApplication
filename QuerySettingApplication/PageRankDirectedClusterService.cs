using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
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
        private Matrix<float> A;
        private int[] _cluster;
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
            A = Matrix<float>.Build.Dense(_numV, _numV);
            foreach (var vertex in graph.Vertexes)
            {
                _cluster[vertex.Id] = vertex.Cluster;
            }

            foreach (var edge in graph.Edges)
            {
                _inDegree[edge.target]++;
                _outDegree[edge.source]++;

                A[edge.source, edge.target] = 1;
            }

            var G = Matrix<float>.Build.Dense(_numV, _numV, CalcGij);
            var t = Vector<float>.Build.Dense(_numV);
            t.At(0, 1);

            Vector<float> k;

            do
            {
                k = t;
                t = t*G;
                //Console.Write(t.ToString());
                //Console.WriteLine();
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
            var f = sum == 0 ? 0.0 : alpha*A[i, j] / sum;
            return (float) (f + (alpha * a + 1.0 - alpha) / _numV);
        }

        private double CalcGijMarkov(int i, int j)
        {
            if (A[i, j] == 0)
                return 0;

            var sum = 0.0;
            for (int k = 0; k < _numV; k++)
                sum += A[i, k];

            return 1 / sum;
        }

        public void Initialize(IGraph graph, int num)
        {
            Initialize(graph as Graph<T>, num);
        }

        private void Initialize(Graph<T> graph, int num)
        {
            int curCl = 0;
            for (var i = 0; i < graph.NumVertexes; )
            {
                for (int j = 0; j < num && i < graph.NumVertexes; j++)
                {
                    _cluster[i] = curCl;
                    i++;
                }
                curCl++;
            }

            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        public void Renumber()
        {
            var cluster = new int[_numV];
            var convertNumbers = new Dictionary<int, int>();
            var curNum = 0;

            for (int i = 0; i < _numV; i++)
            {
                if (!convertNumbers.ContainsKey(_cluster[i]))
                {
                    convertNumbers.Add(_cluster[i], curNum);
                    curNum++;
                }

                cluster[i] = convertNumbers[_cluster[i]];
            }

            _cluster = cluster;
        }

        public int GetContainigCluster(int id)
        {
            return _cluster[id];
        }

        public int NumClusters()
        {
            if (_cluster == null)
                return 0;
            return _cluster.Max() + 1;
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

        internal double Weight(int source, int target)
        {
            return A[source, target];
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

        internal double Weight(int source, List<int> targets)
        {
            return targets.Sum(target => A[source, target]);
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
            float result = 0;
            for (int i = 0; i < _numV; i++)
            {
                for (int j = 0; j < _numV; j++)
                {
                    result += P[i, j] * IsInSameCluster(i, j);
                }
            }

            _modilarity = result;

            using (var s = File.AppendText(_logPath))
            {
                s.WriteLine(_modilarity);
            }
        }

        public float DeltaWeightOfMerge(int C, int D)
        {
            var Cs = new List<int>();
            var Ds = new List<int>();

            for (int i = 0; i < _numV; i++)
            {
                if (_cluster[i] == C)
                {
                    Cs.Add(i);
                    continue;
                }
                if (_cluster[i] == D)
                    Ds.Add(i);
            }
            var res = (from i in Cs from j in Ds select P[i, j]).Sum();
            res += (from i in Ds from j in Cs select P[i, j]).Sum();
            return res;
        }

        public float DeltaWeightOfMoving(int v, int D)
        {
            var C = _cluster[v];

            float result = 0;

            var vC = new List<int>();
            var vD = new List<int>();
            var fvD = (float) 0.0;
            var fvC = (float) 0.0;

            for (int i = 0; i < _numV; i++)
            {
                if (_cluster[i] == C && i != v)
                {
                    vC.Add(i);
                    fvC += P[i, v];
                    fvC += P[v, i];
                    continue;
                }
                if (_cluster[i] == D)
                {
                    vD.Add(i);
                    fvD += P[i, v];
                    fvD += P[v, i];
                }
            }

            result = fvD - fvC;

            return result;
        }

        internal void Move(int v, int D)
        {
            var C = _cluster[v];
            _cluster[v] = D;

            RecalcWeightOfClustering();
            _vertexMovePriotizer.OnMove(v, C, D, this);
        }

        internal void Merge(int C, int D)
        {
            for (int i = 0; i < _numV; i++)
            {
                if (_cluster[i] == D)
                    _cluster[i] = C;
            }
            RecalcWeightOfClustering();

            _mergePriotizer.OnMerge(C, D, this);
        }
    }
}
