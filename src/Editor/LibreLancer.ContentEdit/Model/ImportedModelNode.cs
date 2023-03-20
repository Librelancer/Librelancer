using System.Collections.Generic;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedModelNode
{
    public string Name;
    
    public bool ParentTransform = false;
    public bool Transform = true;
    public List<ModelNode> LODs = new List<ModelNode>();
    public List<Material> Materials = new List<Material>();
    public List<ImportedModelNode> Children = new List<ImportedModelNode>();
    
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