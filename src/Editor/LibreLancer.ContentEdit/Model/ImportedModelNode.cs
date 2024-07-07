using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedModelNode
{
    public string Name;

    public List<ModelNode> LODs = new List<ModelNode>();
    public List<ModelNode> Hulls = new List<ModelNode>();
    public ModelNode Wire;
    public List<HardpointDefinition> Hardpoints = new List<HardpointDefinition>();
    public List<ImportedModelNode> Children = new List<ImportedModelNode>();

    public AbstractConstruct Construct;
    // Have construct properties been set to non-default values
    public bool ConstructPropertiesSet;
    public Transform3D Transform =>
        Construct?.LocalTransform ?? Transform3D.Identity;

    ModelNode def;
    public ModelNode Def {
        get {
            if (LODs.Count > 0) return LODs[0];
            else return def;
        } set {
            def = value;
        }
    }
}
