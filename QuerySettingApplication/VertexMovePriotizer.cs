using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuerySettingApplication
{
    internal class VertexMovePriotizer// : IPriotizer
    {
        class WeigthedPair : IComparable
        {
            public int V;
            public int D;
            public double Weigth;

            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;

                var otherPair = obj as WeigthedPair;
                if (otherPair != null)
                    return otherPair.Weigth.CompareTo(Weigth);
                else
                    throw new ArgumentException("Object is not a Temperature");
            }
        }

        private List<WeigthedPair> _pairs = new List<WeigthedPair>();//= new Dictionary<ClusterPair, double>();
        private List<int> _deadVertexes= new List<int>(); 
        private int numV;
        private int numC;
        //private bool _inited = false;
        internal void Initialize(IClusterService service)
        {
            numV = service.NumVertexes();
            numC = service.NumClusters();

            for (int i = 0; i < numV; i++)
            {
                for (int j = 0; j < numC; j++)
                {
                    if (service.GetContainigCluster(i) == j)
                        continue;

                    var w = service.DeltaWeightOfMoving(i, j);
                    if (w >= 0)
                        _pairs.Add(new WeigthedPair() { V = i, D = j, Weigth = w });
                }
            }

            _pairs.Sort();
            //_inited = true;
        }

        internal double GetPrioritizedPair(out int V, out int D)
        {
            V = 0;
            D = 0;

            var conf = _pairs.FirstOrDefault();
            if (conf == null)
                return -1;

            V = conf.V;
            D = conf.D;
            return conf.Weigth;
        }

        internal void OnMove(int V, int C, int D, IClusterService service)
        {
            //if (!_inited)
            //    return;
            _deadVertexes.Add(V);
            _pairs.RemoveAll(t => t.D == D || t.D == C || t.V == V);
            
            for (int i = 0; i < numV; i++)
            {
                if (i == V || service.GetContainigCluster(i) == C || _deadVertexes.Contains(i)) 
                    continue;

                var w = service.DeltaWeightOfMoving(i, C);
                if (w < 0)
                    continue;

                var p1 = new WeigthedPair() { V = i, D = C, Weigth = w };

                var ind1 = _pairs.Count;
                var isNeed1 = p1.Weigth > 0;
                bool isInd1 = !isNeed1;

                for (int j = 0; j < _pairs.Count; j++)
                {
                    if (!isInd1)
                        if (_pairs[j].Weigth < p1.Weigth)
                        {
                            ind1 = j;
                            isInd1 = true;
                        }

                    if (isInd1)
                        break;
                }

                if (isNeed1)
                    _pairs.Insert(ind1, p1);
            }

            for (int i = 0; i < numV; i++)
            {
                if (i == V || service.GetContainigCluster(i) == D) 
                    continue;

                var w = service.DeltaWeightOfMoving(i, D);
                if (w < 0)
                    continue;

                var p1 = new WeigthedPair() { V = i, D = D, Weigth = w };

                var ind1 = _pairs.Count;
                var isNeed1 = p1.Weigth > 0;
                bool isInd1 = !isNeed1;

                for (int j = 0; j < _pairs.Count; j++)
                {
                    if (!isInd1)
                        if (_pairs[j].Weigth < p1.Weigth)
                        {
                            ind1 = j;
                            isInd1 = true;
                        }

                    if (isInd1)
                        break;
                }

                if (isNeed1)
                    _pairs.Insert(ind1, p1);
            }
        }
    }
}
