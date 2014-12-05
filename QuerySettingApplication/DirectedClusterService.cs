using System;
using System.Collections.Generic;
using System.Linq;
using Gecko;

namespace QuerySettingApplication
{
    public class DirectedClusterService : IClusterService
    {
        //public Clustering Clustering { get; set; }

        //private List<EdgeWrapper> Edges = new List<EdgeWrapper>();

        private MergePriotizer _mergePriotizer = new MergePriotizer();
        //private VertexMovePriotizer _vertexMovePriotizer = new VertexMovePriotizer();
        private bool IsAccurate = false;

        private int _numV;
        private int _numE;
        private double _modilarity;
        private double[] _inDegree;
        private double[] _outDegree;
        private int[,] _matr;
        private int[] _cluster;

        public void Initialize(Graph graph, int num)
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
            for (var i = 0; i < graph.NumVertexes;)
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
        private void Renumber()
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
                
                //var oldMod = _modilarity;
                //deltaMax = DeltaWeightOfMerge(C, D);
                Merge(C, D);
                //var newMod = _modilarity;
                //var actDelta = newMod - oldMod;

                ServiceSingletons.ClusterWindow.SetModularity(_modilarity);

            } while (true);

            Renumber();
        }

        /*public void SSG()
        {
            _mergePriotizer.Initialize(this);
            double lastMod = WeightOfClustering();

            do
            {
                int C, D;
                _mergePriotizer.GetPrioritizedPair(out C, out D);
                var delta = DeltaWeightOfMerge(Clustering[C], Clustering[D]);

                Merge(Clustering[C], Clustering[D]);

                var currM = WeightOfClustering();
                var deltaM = currM - lastMod;
                lastMod = currM;

                ServiceSingletons.ClusterWindow.SetModularity(currM);

                if (deltaM < 0 || Clustering.Count(t => t.Count > 0) == 4)
                    break;
            } while (true);

            Renumber();
        }*/

        /*private void Renumber()
        {
            Clustering.RemoveAll(t => !t.Any());
            Cluster.TotalCount = 0;
            foreach (var cl in Clustering)
            {
                cl.Number = Cluster.TotalCount;
                Cluster.TotalCount++;
            }
        }*/

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

        /*internal int Weight(Cluster sources, Cluster targets)
        {
            return sources.Sum(source => Weight(source, targets));
        }*/

        //private Dictionary<Cluster, double> _degressCluster = new Dictionary<Cluster, double>();

        /*internal double Degree(Cluster vertexes)
        {
            if (!_degressCluster.ContainsKey(vertexes))
                _degressCluster.Add(vertexes, Weight(vertexes, vertexes));

            return _degressCluster[vertexes];
        }*/

        /*internal double TempDegree(Cluster vertexes)
        {
            return Weight(vertexes, vertexes);
        }*/

        /*internal void ReCalcDegree(Cluster vertexes)
        {
            if (!_degressCluster.ContainsKey(vertexes))
                _degressCluster.Add(vertexes, Weight(vertexes, vertexes));
            else
                _degressCluster[vertexes] = Weight(vertexes, vertexes);
        }*/

        public int NumVertexes()
        {
            return _numV;
        }

        internal double EdgeCount()
        {
            return _numE;
        }

        internal double WeightOfClustering()
        {
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
            /*if (IsAccurate)
            {
                var result = WeightOfClustering();

                var newCl = new Cluster();
                newCl.AddRange(C);
                newCl.AddRange(D);
                Clustering.Remove(C);
                Clustering.Remove(D);
                Clustering.Add(newCl);

                result = WeightOfClustering() - result;

                Clustering.Add(C);
                Clustering.Add(D);
                Clustering.Remove(newCl);

                return result;
            }
            else*/
                //return 2 * Weight(C, D) / DegreeVertexes() - 2 * Degree(C) * Degree(D) / Math.Pow(DegreeVertexes(), 2);
            //return Weight(C, D) / EdgeCount() - Degree(C) * Degree(D) / Math.Pow(EdgeCount(), 2);
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
        /*
        internal Cluster GetTempCluster(Cluster C, int removingVertex)
        {
            var result = new TempCluster();
            result.AddRange(C.Where(t => t != removingVertex));
            return result;
        }*/
        /*
        public double DeltaWeightOfMoving(int v, Cluster D)
        {
            return 0;
        }*/

        internal void Move(int v, int C)
        {
            _cluster[v] = C;
            /*var D = Clustering.GetContainigCluster(v);
            D.Remove(v);

            C.Add(v);
            ReCalcDegree(C);
            ReCalcDegree(D);*/
            RecalcWeightOfClustering();

            //_vertexMovePriotizer.OnMove(v, C, D, this);
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
        //double DeltaWeightOfMoving(int i, int cluster);

        void Initialize(Graph graph, int num);
        //void CG();
        void SSG();
        int GetContainigCluster(int id);
        int NumClusters();
    }
}
