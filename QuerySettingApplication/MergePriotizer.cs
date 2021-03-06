﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace QuerySettingApplication
{
    internal class MergePriotizer<T> where T : Vertex, new() // : IPriotizer
    {
        class WeigthedPair : IComparable
        {
            public int C;
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
        private int _num;

        private struct MyStruct
        {
            internal int A;
            internal int B;
        }

        internal void Initialize(IClusterService service)
        {
            _num = service.NumClusters();

            foreach (var edge in service.Graph.Edges)
            {
                var i = service.GetContainigCluster(edge.source);
                var j = service.GetContainigCluster(edge.target);
                if (i == j)
                    continue;
                var w = service.DeltaWeightOfMerge(i, j);

                if (w > 0)
                    _pairs.Add(new WeigthedPair() { C = i, D = j, Weigth = w });
            }

            _pairs.Sort();
        }

        internal double GetPrioritizedPair(out int C, out int D)
        {
            C = 0;
            D = 0;
            
            var conf = _pairs.FirstOrDefault();
            if (conf == null)
                return -1;

            C = conf.C;
            D = conf.D;
            return conf.Weigth;
        }

        internal List<KeyValuePair<int, int>> GetTopPrioritizedPairs(int l)
        {
            var result = new List<KeyValuePair<int, int>>();

            for (int i = 0; i < _pairs.Count && i < l; i++)
            {
                result.Add(new KeyValuePair<int, int>(_pairs[i].C, _pairs[i].D));
            }

            return result;
        }

        internal void OnMerge(int C, int D, IClusterService service)
        {
            _pairs.RemoveAll(t => t.D == D || t.D == C || t.C == C || t.C == D);

            //var list = new List<int>();
            //for (int i = 0; i < _num; i++)
            //    if (service.GetContainigCluster(i) == C)
            //        list.Add(i);

            var list = service.GetIndClusters(C);

            foreach (var d in list)
            {
                if (C == d)
                    continue;
                var w = service.DeltaWeightOfMerge(C, d);

                if (w > 0)
                    _pairs.Add(new WeigthedPair() { C = C, D = d, Weigth = w });
            }

            /*foreach (var edge in service.Graph.Edges)
            {
                if (!list.Contains(edge.target))
                    continue;

                var j = edge.source;

                var w = service.DeltaWeightOfMerge(j, C);

                if (w > 0)
                    _pairs.Add(new WeigthedPair() { C = j, D = C, Weigth = w });
            }*/

            /*
            for (int i = 0; i < _num; i++)
            {    
                if (C != i)
                {
                    var p1 = new WeigthedPair() { C = C, D = i, Weigth = service.DeltaWeightOfMerge(C, i)};
                    var p2 = new WeigthedPair() { C = i, D = C, Weigth = service.DeltaWeightOfMerge(i, C)};
                    var ind1 = _pairs.Count;
                    var isNeed1 = p1.Weigth > 0;
                    bool isInd1 = !isNeed1;
                    var ind2 = _pairs.Count;
                    var isNeed2 = p2.Weigth > 0;
                    bool isInd2 = !isNeed2;

                    for (int j = 0; j < _pairs.Count; j++)
                    {
                        if (!isInd1)
                            if (_pairs[j].Weigth < p1.Weigth)
                            {
                                ind1 = j;
                                isInd1 = true;
                            }

                        if (!isInd2)
                            if (_pairs[j].Weigth < p2.Weigth)
                            {
                                ind2 = j;
                                isInd2 = true;
                            }

                        if (isInd1 && isInd2)
                            break;
                    }

                    if (isNeed1)
                        _pairs.Insert(ind1, p1);
                    if (isNeed2)
                        _pairs.Insert(ind2, p2);
                }
            }*/
            

            _pairs.Sort();
        }

        public int NumPozitivePairs()
        {
            return _pairs.Count;
        }
    }
}
