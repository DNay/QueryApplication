using System;
using System.Collections.Generic;
using System.Linq;
using Gecko;

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
        private double _modilarity;
        private double[] _inDegree;
        private double[] _outDegree;
        private int[,] _matr;
        private int[] _cluster;

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
            _matr = new int[_numV, _numV];

            foreach (var vertex in graph.Vertexes)
            {
                _cluster[vertex.Id] = vertex.Cluster;
            }

            foreach (var edge in graph.Edges)
            {
                _inDegree[edge.target]++;
                _outDegree[edge.source]++;

                _matr[edge.source, edge.target] = 1;
            }
            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());
        }

        public void Initialize(IGraph graph, int num)
        {
            Initialize(graph as Graph<T>, num);
        }

        private void Initialize(Graph<T> graph, int num)
        {

            //Clustering.Clear();
            //Cluster.TotalCount = 0;

            _numV = graph.NumVertexes;
            _numE = graph.Edges.Count;
            _inDegree = new double[_numV];
            _outDegree = new double[_numV];
            _cluster = new int[_numV];
            _matr = new int[_numV, _numV];

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

            foreach (var edge in graph.Edges)
            {
                _inDegree[edge.target]++;
                _outDegree[edge.source]++;

                _matr[edge.source, edge.target] = 1;
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

            /*
            var testV = 5;
            var testC = _cluster[testV];
            var testD = 2;

            var test = DeltaWeightOfMoving(testV, testD, 0);
            var oldMod = _modilarity;
            Move(testV, testD);
            var newMod = _modilarity;
            var actDelta = newMod - oldMod;
            Move(testV, testC);
            newMod = _modilarity;
            actDelta = newMod - oldMod;

            test = DeltaWeightOfMoving(testV, testD, 1);
            oldMod = _modilarity;
            Move(testV, testD);
            newMod = _modilarity;
            actDelta = newMod - oldMod;
            Move(testV, testC);
            newMod = _modilarity;
            actDelta = newMod - oldMod;

            test = DeltaWeightOfMoving(testV, testD, 2);
            oldMod = _modilarity;
            Move(testV, testD);
            newMod = _modilarity;
            actDelta = newMod - oldMod;
            Move(testV, testC);
            newMod = _modilarity;
            actDelta = newMod - oldMod;

            test = DeltaWeightOfMoving(testV, testD, 3);
            oldMod = _modilarity;
            Move(testV, testD);
            newMod = _modilarity;
            actDelta = newMod - oldMod;
            Move(testV, testC);
            newMod = _modilarity;
            actDelta = newMod - oldMod;*/
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
                    result += (_matr[i, j] - (_outDegree[i] * _inDegree[j]) / EdgeCount()) * IsInSameCluster(i, j);
                }
            }

            _modilarity = result / EdgeCount();
        }

        public double DeltaWeightOfMerge(int C, int D)
        {
            double result = 0;

            var outs = new List<int>();
            var ins = new List<int>();

            for (int i = 0; i < _numV; i++)
            {
                if (_cluster[i] == C)
                {
                    outs.Add(i);
                    continue;
                }
                if (_cluster[i] == D)
                    ins.Add(i);
            }

            foreach (var i in ins)
            {
                foreach (var j in outs)
                {
                    result += (_matr[i, j] - (_outDegree[i] * _inDegree[j]) / EdgeCount());
                }
            }

            return result / EdgeCount();
        }

        public double DeltaWeightOfMoving(int v, int D)
        {
            /*var C = _cluster[v];
            var oldMod = _modilarity;
            Move(v, D);
            var res = _modilarity - oldMod;
            Move(v, C);
            return res;*/

            var C = _cluster[v];

            double result = 0;

            var vC = new List<int>();
            var vD = new List<int>();
            var fvD = 0;
            var fvC = 0;

            for (int i = 0; i < _numV; i++)
            {
                if (_cluster[i] == C && i != v)
                {
                    vC.Add(i);
                    fvC += _matr[i, v];
                    continue;
                }
                if (_cluster[i] == D)
                {
                    vD.Add(i);
                    fvD += _matr[v, i];
                }
            }

            result += fvD - fvC;

            switch (0)
            {
                case 0:
                    foreach (var i in vD)
                    {
                        result -= (_outDegree[v] * _inDegree[i]) / EdgeCount();
                    }

                    foreach (var j in vC)
                    {
                        result += (_outDegree[j] * _inDegree[v]) / EdgeCount();
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

    public interface IClusterService
    {
        //Clustering Clustering { get; set; }
        int NumVertexes();
        double DeltaWeightOfMerge(int cl1, int cl2);
        double DeltaWeightOfMoving(int i, int cluster);
        void SetGraph(IGraph graph);

        void Initialize(IGraph graph, int num);
        void CG();
        void SSG();
        int GetContainigCluster(int id);
        int NumClusters();
        void Renumber();

        double WeightOfClustering();
    }
}
