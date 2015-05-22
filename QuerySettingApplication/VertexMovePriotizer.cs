using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuerySettingApplication
{
    internal class VertexMovePriotizer<T> where T : Vertex, new()// : IPriotizer
    {
        internal class WeigthedPair : IComparable
        {
            public int V;
            public int D;
            public float Weigth;

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

        public List<WeigthedPair> Pairs
        {
            get { return _pairs; }
        }

        //private bool _inited = false;
        internal void Initialize(IClusterService service)
        {
            _deadVertexes.Clear();
            numV = service.NumVertexes();
            numC = service.NumClusters();

            for (int i = 0; i < numV; i++)
            {
                for (int j = 0; j < numC; j++)
                {
                    if (service.GetContainigCluster(i) == j)
                        continue;

                    var w = service.DeltaWeightOfMoving(i, j);
                    if (w > 0)
                        _pairs.Add(new WeigthedPair() { V = i, D = j, Weigth = w });
                }
            }

            _pairs.Sort();
            //_inited = true;
        }

        internal float GetPrioritizedPair(out int V, out int D)
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

        internal float GetPrioritizedPair(bool[] moved, out int V, out int D)
        {
            V = 0;
            D = 0;

            var conf = _pairs.FirstOrDefault(t => !moved[t.V]);
            if (conf == null)
                return -1;

            V = conf.V;
            D = conf.D;
            return conf.Weigth;
        }

        public float GetBestCluster(int v, out int c)
        {
            c = 0;
            var p = _pairs.FirstOrDefault(t => t.V == v);
            if (p == null)
                return -1;

            c = p.D;
            return p.Weigth;
        }

        internal void OnMove(int V, int C, int D, IClusterService service)
        {
            //if (!_inited)
            //    return;
            _deadVertexes.Add(V);
            _pairs.RemoveAll(t => t.D == D || t.D == C || t.V == V);

            var listV = service.GetIndVertexes(V);

            if (listV != null)
                foreach (var v in listV)
                {
                    var w = service.DeltaWeightOfMoving(v, C);
                    if (w > 0)
                        _pairs.Add(new WeigthedPair() { V = v, D = C, Weigth = w });

                    w = service.DeltaWeightOfMoving(v, D);
                    if (w > 0)
                        _pairs.Add(new WeigthedPair() { V = v, D = D, Weigth = w });
                }

            _pairs.Sort();
        }
    }
}
