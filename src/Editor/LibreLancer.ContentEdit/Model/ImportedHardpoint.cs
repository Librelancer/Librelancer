using System.Collections.Generic;
using LibreLancer.Utf.Cmp;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedHardpoint(HardpointDefinition hardpoint)
{
    public HardpointDefinition Hardpoint = hardpoint;
    public List<ModelNode> Hulls = new List<ModelNode>();
}
