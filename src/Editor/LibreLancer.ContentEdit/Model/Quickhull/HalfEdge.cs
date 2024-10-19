using System.Numerics;
using static LibreLancer.ContentEdit.Model.Quickhull.QHDebug;

namespace LibreLancer.ContentEdit.Model.Quickhull;

class HalfEdge(Vertex vertex, Face face)
{
    public Vertex Vertex = vertex;
    public Face Face = face;
    public HalfEdge Next;
    public HalfEdge Prev;
    public HalfEdge Opposite;

    public Vertex Head() => Vertex;
    public Vertex Tail() => Prev?.Vertex;

    public float Length() => Tail() == null
        ? -1
        : Vector3.Distance(Tail().Point, Head().Point);

    public float LengthSquared() => Tail() == null
        ? -1
        : Vector3.DistanceSquared(Tail().Point, Head().Point);

    public void SetOpposite(HalfEdge edge)
    {
        /*if (IsDebug)
        {
            debug(
                $"opposite {Tail().Index} <--> {Head().Index} between {Face.ToPrintString()}, {edge.Face.ToPrintString()}"
            );
        }*/
        Opposite = edge;
        edge.Opposite = this;
    }
}
