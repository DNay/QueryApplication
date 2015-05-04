using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace QuerySettingApplication
{
    [JsonObject]
    [Serializable]
    [XmlInclude(typeof(VertexPublication))]
    public class Vertex
    {
        public Vertex()
        { }

        public Vertex(string name)
        {
            Name = name;
        }

        [JsonProperty]
        [XmlElement("Id")] 
        public int Id{ get; set; }

        [JsonProperty]
        [XmlElement("Cluster")] 
        public int Cluster { get; set; }

        [JsonProperty]
        [XmlElement("Name")]
        public string Name { get; set; }

        [JsonProperty]
        public double X { get; set; }

        [JsonProperty]
        public double Y { get; set; }

        [JsonProperty]
        public int degreeIn { get; set; }

        public override bool Equals(object obj)
        {
            var otherVertex = obj as Vertex;
            return otherVertex != null && otherVertex.Id == Id;
        }

        [JsonProperty]
        [XmlElement("Infos")]
        public List<RdfInfo> Infos { get; set; }
    }

    [JsonObject]
    [Serializable]
    public class RdfInfo
    {
        protected bool Equals(RdfInfo other)
        {
            return string.Equals(Predicate, other.Predicate) && string.Equals(Subject, other.Subject);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Predicate != null ? Predicate.GetHashCode() : 0)*397) ^ (Subject != null ? Subject.GetHashCode() : 0);
            }
        }

        public RdfInfo()
        { }

        public RdfInfo(string p, string s)
        {
            Predicate = p;
            Subject = s;
        }

        [JsonProperty]
        [XmlElement("Predicate")]
        public string Predicate { get; set; }
        [JsonProperty]
        [XmlElement("Subject")]
        public string Subject { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RdfInfo) obj);
        }
    }

    [JsonObject]
    [Serializable]
    public class VertexPublication : Vertex
    {
        public VertexPublication()
        { }
        public VertexPublication(string name, List<string> authors = null, DateTime date = default(DateTime)) : base(name)
        {
            Authors = authors;
            Date = date;
        }

        [JsonProperty]
        [XmlElement("Authors")]
        public List<string> Authors { get; set; }

        [JsonProperty]
        [XmlElement("Date")]
        public DateTime Date { get; set; }
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

        [JsonProperty]
        public double fictX { get; set; }
        [JsonProperty]
        public double fictY { get; set; }

        [JsonProperty]
        public double weight { get; set; }


        public override bool Equals(object obj)
        {
            var otherEdge = obj as Edge;
            return otherEdge != null && (otherEdge.target == target && otherEdge.source == source);
        }
    }

    public interface IGraph
    {
        List<Vertex> Vertexes { get; set; }
        List<Edge> Edges { get; set; }
    }

    [JsonObject]
    [Serializable]
    public class Graph<T> : IGraph where T : Vertex, new()
    {
        public Graph()
        {
            NumVertexes = 0;
            Vertexes = new List<Vertex>();
            Edges = new List<Edge>();
        }

        public virtual T AddVertex(string name)
        {
            var cur = GetVertex(name);
            if (cur != null)
            {
                return cur;
            }

            var newVer = new T {Id = NumVertexes++, Name = name};
            Vertexes.Add(newVer);
            return newVer;
        }

        public Edge AddEdge(Edge edge, bool isSearch = true)
        {
            if (isSearch)
            {
                var cur = GetEdge(edge);
                if (cur != null)
                {
                    return cur;
                }
            }

            Edges.Add(edge);
            return edge;
        }

        public virtual T GetVertex(string name)
        {
            return Vertexes.FirstOrDefault(t => t.Name == name) as T;
        }
        public Edge GetEdge(Edge edge)
        {
            return Edges.FirstOrDefault(t => t.source == edge.source && t.target == edge.target);
        }

        public int GetVertexId(string name)
        {
            var firstOrDefault = Vertexes.FirstOrDefault(t => t.Name == name);
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

    [JsonObject]
    [Serializable]
    public class CiteNet : Graph<VertexPublication>
    {
        public virtual VertexPublication AddVertex(string name, List<string> authors, DateTime date)
        {
            var newVert = base.AddVertex(name);
            newVert.Authors = authors;
            newVert.Date = date;

            return newVert;
        }
    }

    [JsonObject]
    [Serializable]
    public class AuthorsGraph : Graph<Vertex>
    {
    }
}
