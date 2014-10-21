using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace QuerySettingApplication
{
    [Serializable]
    public class Vertex
    {
        public Vertex(){ }

        public Vertex(string text)
        {
            Texts = new List<string> {text};
        }

        [XmlElement("Id")] 
        public int Id{ get; set; }
        [XmlElement("Cluster")] 
        public int Cluster { get; set; }
        [XmlElement("Texts")] 
        public List<string> Texts { get; set; }

        public override bool Equals(object obj)
        {
            var otherVertex = obj as Vertex;
            return otherVertex != null && otherVertex.Id == Id;
        }
    }

    [Serializable]
    public class Edge
    {
        public Edge() { }

        public Edge(int outV, int inV)
        {
            InId = inV;
            OutId = outV;
        }

        [XmlElement("InId")] 
        public int InId { get; set; }
        [XmlElement("OutId")] 
        public int OutId { get; set; }
        public override bool Equals(object obj)
        {
            var otherEdge = obj as Edge;
            return otherEdge != null && (otherEdge.InId == InId && otherEdge.OutId == OutId);
        }
    }

    [Serializable]
    public class Graph
    {
        public Graph()
        {
            NumVertexes = 0;
            Vertexes = new List<Vertex>();
            Edges = new List<Edge>();
        }

        public Vertex AddVertex(string name)
        {
            var cur = GetVertex(name);
            if (cur != null)
            {
                return cur;
            }

            var newVer = new Vertex(name) {Id = NumVertexes++};
            Vertexes.Add(newVer);
            return newVer;
        }

        public Vertex GetVertex(string name)
        {
            return Vertexes.FirstOrDefault(t => t.Texts.Contains(name));
        }

        public int GetVertexId(string name)
        {
            var firstOrDefault = Vertexes.FirstOrDefault(t => t.Texts.Contains(name));
            return firstOrDefault != null ? firstOrDefault.Id : -1;
        }

        [XmlElement("NumVertexes")]
        public int NumVertexes { get; set; }
        [XmlElement("Vertexes")]
        public List<Vertex> Vertexes { get; set; }
        [XmlElement("Edges")]
        public List<Edge> Edges { get; set; }
    }
}
