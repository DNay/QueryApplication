using System;
using System.Collections.Generic;
using System.Linq;

namespace QuerySettingApplication
{
    internal struct EdgeWrapper
    {
        public int source;
        public int target;
    }

    /*public class Cluster : List<int>
    {
        public Cluster()
        {
            Number = TotalCount;
            TotalCount++;
        }

        public int Number
        {
            get;
            set;
        }

        public static int TotalCount = 0;
    }

    internal class TempCluster : Cluster
    {
        public TempCluster()
        {
        }
    }


    public class Clustering : List<Cluster>
    {
        public Cluster GetContainigCluster(int vertex)
        {
            return Find(t => t.Contains(vertex));
        }
    }*/
    /*internal struct ClusterPair
    {
        public Cluster C;
        public Cluster D;

        public bool Contains(Cluster cl)
        {
            return cl == C || cl == D;
        }

        public override bool Equals(object obj)
        {
            var pair = obj is ClusterPair ? (ClusterPair)obj : new ClusterPair();
            return (C == pair.C && D == pair.D);// || (D == pair.C && C == pair.D);
        }
    }*/

    //internal interface IPriotizer
    //{
    //   ClusterPair GetPrioritizedPair(ClusterService service);
    //}



    public class ClusterService : IClusterService
    {
        public Clustering Clustering { get; set; }

        //private List<EdgeWrapper> Edges = new List<EdgeWrapper>();

        private MergePriotizer _mergePriotizer = new MergePriotizer();
        private VertexMovePriotizer _vertexMovePriotizer = new VertexMovePriotizer();
        private bool IsAccurate = false;

        private int _numV;
        private int _numE;
        private double _modilarity;

        public void Initialize(Graph graph, int num)
        {
            Clustering = new Clustering();
            Cluster.TotalCount = 0;
            for (var i = 0; i < graph.NumVertexes;)
            {
                var cl = new Cluster();
                for (int j = 0; j < num && i < graph.NumVertexes; j++)
                {
                    cl.Add(i);
                    i++;
                }
                Clustering.Add(cl);
            }

            _numV = graph.NumVertexes;
            _numE = graph.Edges.Count;
            _degress = new int[_numV];
            _matr = new int[_numV, _numV];

            foreach (var edge in graph.Edges)
            {
                _matr[edge.source, edge.target] = 1;
                _degress[edge.target]++;
            }
            RecalcWeightOfClustering();
            ServiceSingletons.ClusterWindow.SetModularity(WeightOfClustering());


            /*for (var i = 0; i < graph.NumVertexes; ++i)
            {
                //_degress.Add(Edges.Count(t => t.target == i));
                //_degress[i] = Edges.Count(t => t.target == i);
                for (var j = 0; j < graph.NumVertexes; ++j)
                {
                    _matr[i, j] = Edges.Contains(new EdgeWrapper {source = j, target = i}) ? 1 : 0;
                }  
            }*/

            /*var newCl = GetTempCluster(Clustering.First(), 0);

            _weightOfClustering = WeightOfClustering();

            Console.WriteLine(DegreeVertexes());

            Console.WriteLine(WeightOfClustering());
            Console.WriteLine(DeltaWeightOfMerge(Clustering[0], Clustering[1]));
            //Merge(Clustering[0], Clustering[1]);
            Console.WriteLine(_weightOfClustering);
            Console.WriteLine(WeightOfClustering());

            Console.WriteLine(DeltaWeightOfMoving(0, Clustering[1]));
            //Move(0, Clustering[1]);
            Console.WriteLine(_weightOfClustering);
            Console.WriteLine(WeightOfClustering());*/
        }

        public void CG()
        {
            _vertexMovePriotizer.Initialize(this);
            double lastMod = WeightOfClustering();
            do
            {
                int V, D;
                _vertexMovePriotizer.GetPrioritizedPair(out V, out D);
                var delta = DeltaWeightOfMoving(V, Clustering[D]);

                Move(V, Clustering[D]);

                var currM = WeightOfClustering();
                var deltaM = currM - lastMod;
                lastMod = currM;

                ServiceSingletons.ClusterWindow.SetModularity(currM);

                if (delta < 0 || Clustering.Count(t => t.Count > 0) == 4)
                    break;
            } while (true);

            Renumber();
        }

        public void SSG()
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
        }

        private void Renumber()
        {
            Clustering.RemoveAll(t => !t.Any());
            Cluster.TotalCount = 0;
            foreach (var cl in Clustering)
            {
                cl.Number = Cluster.TotalCount;
                Cluster.TotalCount++;
            }
        }

        internal int Weight(int source, int target)
        {
            return _matr[source, target];
        }

        private int[] _degress;
        private int[,] _matr;

        internal int Degree(int vertex)
        {
            return _degress[vertex];
        }

        internal int Weight(int source, List<int> targets)
        {
            return targets.Sum(target => _matr[source, target]);
        }

        internal int Weight(Cluster sources, Cluster targets)
        {
            return sources.Sum(source => Weight(source, targets));
        }

        private Dictionary<Cluster, double> _degressCluster = new Dictionary<Cluster, double>();

        internal double Degree(Cluster vertexes)
        {
            if (!_degressCluster.ContainsKey(vertexes))
                _degressCluster.Add(vertexes, Weight(vertexes, vertexes));

            return _degressCluster[vertexes];
        }

        internal double TempDegree(Cluster vertexes)
        {
            return Weight(vertexes, vertexes);
        }

        internal void ReCalcDegree(Cluster vertexes)
        {
            if (!_degressCluster.ContainsKey(vertexes))
                _degressCluster.Add(vertexes, Weight(vertexes, vertexes));
            else
                _degressCluster[vertexes] = Weight(vertexes, vertexes);
        }

        public int NumVertexes()
        {
            return _numV;
        }

        internal double DegreeVertexes()
        {
            return _numE;
        }

        internal double WeightOfClustering()
        {
            return _modilarity;
        }

        internal void RecalcWeightOfClustering()
        {
            _modilarity = Clustering.Sum(cluster => ((Degree(cluster) / DegreeVertexes()) - (Math.Pow(Degree(cluster), 2) / Math.Pow(DegreeVertexes(), 2))));
        }

        public double DeltaWeightOfMerge(Cluster C, Cluster D)
        {
            if (IsAccurate)
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
            else
                //return 2 * Weight(C, D) / DegreeVertexes() - 2 * Degree(C) * Degree(D) / Math.Pow(DegreeVertexes(), 2);
                return Weight(C, D) / DegreeVertexes() - Degree(C) * Degree(D) / Math.Pow(DegreeVertexes(), 2);
        }

        internal Cluster GetTempCluster(Cluster C, int removingVertex)
        {
            var result = new TempCluster();
            result.AddRange(C.Where(t => t != removingVertex));
            return result;
        }

        public double DeltaWeightOfMoving(int v, Cluster D)
        {
            if (IsAccurate)
            {
                var result = WeightOfClustering();

                var C = Clustering.GetContainigCluster(v);
                var tempC = GetTempCluster(C, v);
                Clustering.Remove(C);
                Clustering.Add(tempC);
                D.Add(v);

                result = WeightOfClustering() - result;

                Clustering.Remove(tempC);
                Clustering.Add(C);
                D.Remove(v);

                return result;
            }
            else
            {
                var C = Clustering.GetContainigCluster(v);
                var tempC = GetTempCluster(C, v);
                return (Weight(v, D) - Weight(v, tempC)) / DegreeVertexes() -
                       Degree(v) * (Degree(D) - TempDegree(tempC)) / Math.Pow(DegreeVertexes(), 2);
                //return 2 * (Weight(v, D) - Weight(v, tempC)) / DegreeVertexes() -
                //       2 * Degree(v) * (Degree(D) - TempDegree(tempC)) / Math.Pow(DegreeVertexes(), 2);
            }
        }

        internal void Move(int v, Cluster C)
        {
            var D = Clustering.GetContainigCluster(v);
            D.Remove(v);

            C.Add(v);
            ReCalcDegree(C);
            ReCalcDegree(D);
            RecalcWeightOfClustering();

            _vertexMovePriotizer.OnMove(v, C, D, this);
        }

        internal void Merge(Cluster C, Cluster D)
        {
            C.AddRange(D);
            D.Clear();
            ReCalcDegree(C);
            ReCalcDegree(D);
            RecalcWeightOfClustering();

            _mergePriotizer.OnMerge(C, D, this);
        }
    }
}
