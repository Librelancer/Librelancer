namespace LibreLancer.ContentEdit.Model.Quickhull;

class VertexList
{
    private Vertex head;
    private Vertex tail;

    public void Clear() => head = tail = null;

    public void InsertBefore(Vertex target, Vertex node)
    {
        node.Prev = target.Prev;
        node.Next = target;
        if (node.Prev == null)
        {
            this.head = node;
        }
        else
        {
            node.Prev.Next = node;
        }
        target.Prev = node;
    }

    public void InsertAfter(Vertex target, Vertex node)
    {
        node.Prev = target;
        node.Next = target.Next;
        if (node.Next == null)
        {
            this.tail = node;
        }
        else
        {
            node.Next.Prev = node;
        }
        target.Next = node;
    }


    public void Add(Vertex node)
    {
        if (head == null)
        {
            head = node;
        }
        else
        {
            tail.Next = node;
        }
        node.Prev = tail;
        node.Next = null;
        tail = node;
    }

    public void AddAll(Vertex node)
    {
        if (head == null)
        {
            head = node;
        }
        else
        {
            tail.Next = node;
        }
        node.Prev = tail;
        while (node.Next != null) {
            node = node.Next;
        }
        tail = node;
    }

    public void Remove(Vertex node)
    {
        if (node.Prev == null)
        {
            this.head = node.Next;
        }
        else
        {
            node.Prev.Next = node.Next;
        }

        if (node.Next == null)
        {
            this.tail = node.Prev;
        }
        else
        {
            node.Next.Prev = node.Prev;
        }
    }

    public void RemoveChain(Vertex a, Vertex b)
    {
        if (a.Prev == null)
        {
            this.head = b.Next;
        }
        else
        {
            a.Prev.Next = b.Next;
        }

        if (b.Next == null)
        {
            this.tail = a.Prev;
        }
        else
        {
            b.Next.Prev = a.Prev;
        }
    }

    public Vertex First  => head;

    public bool IsEmpty => head == null;

}
