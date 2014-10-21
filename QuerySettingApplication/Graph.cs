using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    [JsonObject]
    [Serializable]
    public class Vertex
    {
        public Vertex(){ }

        public Vertex(string text)
        {
            Texts = new List<string> {text};
        }

        [JsonProperty]
        [XmlElement("Id")] 
        public int Id{ get; set; }

        [JsonProperty]
        [XmlElement("Cluster")] 
        public int Cluster { get; set; }

        [JsonProperty]
        [XmlElement("Texts")] 
        public List<string> Texts { get; set; }

        public override bool Equals(object obj)
        {
            var otherVertex = obj as Vertex;
            return otherVertex != null && otherVertex.Id == Id;
        }
    }

    [JsonObject]
    [Serializable]
    public class Edge
    {
        public Edge() { }

        public Edge(int outV, int inV)
        {
            target = inV;
            source = outV;
        }

        [JsonProperty]
        [XmlElement("target")] 
        public int target { get; set; }

        [JsonProperty]
        [XmlElement("source")] 
        public int source { get; set; }
        public override bool Equals(object obj)
        {
            var otherEdge = obj as Edge;
            return otherEdge != null && (otherEdge.target == target && otherEdge.source == source);
        }
    }

    [JsonObject]
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

        [JsonProperty]
        [XmlElement("NumVertexes")]
        public int NumVertexes { get; set; }

        [JsonProperty]
        [XmlElement("Vertexes")]
        public List<Vertex> Vertexes { get; set; }

        [JsonProperty]
        [XmlElement("Edges")]
        public List<Edge> Edges { get; set; }
    }
}
