﻿using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

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
        private double _modilarity;
        private double[] _inDegree;
        private double[] _outDegree;
        private Matrix<double> A;
        private int[] _cluster;
        private Matrix<double> L;
        private Matrix<double> P;
        private Vector<double> pi;

        public void SetGraph(IGraph graph)
        {
            SetGraph(graph as Graph<T>);
        }

        public void SetGraph(Graph<T> graph)
        {
            _numV = graph.NumVertexes;
            _numE = graph.Edges.Count;
            _inDegree = new double[_numV];
            _outDegree = new double[_numV];
            _cluster = new int[_numV];
            A = Matrix<double>.Build.Dense(_numV, _numV);
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

            var G = Matrix<double>.Build.Dense(_numV, _numV, CalcGij);
            var t = Vector<double>.Build.Dense(_numV);
            t.At(0, 1);

            Vector<double> k;

            do
            {
                k = t;
                t = t*G;
                //Console.Write(t.ToString());
                //Console.WriteLine();
            } while ((k - t).Norm(2) > 0.0001);

            pi = t;
            L = Matrix<double>.Build.Dense(_numV, _numV, (i, j) => pi.At(i) * G[i, j]);
            P = Matrix<double>.Build.Dense(_numV, _numV, (i, j) => L[i, j] - pi[i] * pi[j]);

            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        private double CalcGij(int i, int j)
        {
            const double alpha = 0.85;

            var sum = _outDegree[i];
            var a = sum == 0 ? 1.0 : 0.0;
            var f = sum == 0 ? 0.0 : alpha*A[i, j] / sum;
            return f + (alpha * a + 1.0 - alpha) / _numV;
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

        public void MSG(double mergeFactor)
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

        internal double EdgeCount()
        {
            return _numE;
        }

        public double WeightOfClustering()
        {
            if (_modilarity == 0)
                RecalcWeightOfClustering();

            return _modilarity;
        }

        internal void RecalcWeightOfClustering()
        {
            double result = 0;
            for (int i = 0; i < _numV; i++)
            {
                for (int j = 0; j < _numV; j++)
                {
                    result += P[i, j] * IsInSameCluster(i, j);
                }
            }

            _modilarity = result;
            Console.WriteLine(_modilarity);
        }

        public double DeltaWeightOfMerge(int C, int D)
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

        public double DeltaWeightOfMoving(int v, int D)
        {
            var C = _cluster[v];

            double result = 0;

            var vC = new List<int>();
            var vD = new List<int>();
            var fvD = 0.0;
            var fvC = 0.0;

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
