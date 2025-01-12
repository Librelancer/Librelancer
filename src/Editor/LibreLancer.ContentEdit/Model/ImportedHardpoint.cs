using System.Collections.Generic;
using LibreLancer.Utf.Cmp;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedHardpoint
{
    public HardpointDefinition Hardpoint;
    public List<ModelNode> Hulls = new List<ModelNode>();
}
