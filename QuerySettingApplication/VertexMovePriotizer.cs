using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuerySettingApplication
{
    internal class VertexMovePriotizer// : IPriotizer
    {
        private double[,] _pairs; //= new Dictionary<ClusterPair, double>();
        private List<int> _deadClusters = new List<int>();
        private int numV;
        private int numC;
        internal void Initialize(IClusterService service)
        {
            numC = service.Clustering.Count;
            numV = service.NumVertexes();
            _pairs = new double[numV, numC];

            for (int i = 0; i < numV; i++)
            {
                for (int j = 0; j < numC; j++)
                {
                    if (service.Clustering[j].Count == 0)
                    {
                        _deadClusters.Add(j);
                        continue;
                    }

                    if (j == service.Clustering.GetContainigCluster(i).Number)
                        _pairs[i, j] = 0;
                    else
                        _pairs[i, j] = service.DeltaWeightOfMoving(i, service.Clustering[j]);
                }
            }
        }

        internal void GetPrioritizedPair(out int V, out int C)
        {
            V = 0;
            C = 0;
            double max = -1;
            for (int i = 0; i < numV; i++)
            {
                for (int j = 0; j < numC; j++)
                {
                    if (_deadClusters.Contains(j))
                        continue;
                    if (_pairs[i, j] > max)
                    {
                        max = _pairs[i, j];
                        V = i;
                        C = j;
                    }
                }
            }
        }

        internal void OnMove(int V, Cluster C, Cluster D, IClusterService service)
        {
            if (D.Count == 0)
                _deadClusters.Add(D.Number);
            
            for (int i = 0; i < numC; i++)
            {
                if (_deadClusters.Contains(i))
                    continue;
                
                if (C.Number != i)
                {
                    _pairs[V, i] = service.DeltaWeightOfMoving(V, service.Clustering[i]);
                }
                else
                {
                    _pairs[V, i] = 0;
                }
            }
        }
    }
}
