using System;
using System.IO;

namespace LibreLancer.ContentEdit;

public class NodeWriter : BinaryWriter
{
    public NodeWriter() : base(new MemoryStream()) { }

    public LUtfNode GetUtfNode(string name, LUtfNode parent = null)
    {
        return new LUtfNode() {Name = name, Data = ((MemoryStream) BaseStream).ToArray(), Parent = parent};
    }
}
