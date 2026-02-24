using System.Collections.Generic;
using System.Linq;
using LibreLancer.Fx;

namespace LancerEdit;

partial class AleEditor
{
    class AddNodeReference(AleEditor Ed, NodeReference? Parent, NodeReference NewRef) : EditorAction
    {
        public override void Commit()
        {
            if(Parent != null)
                Parent.Children.Add(NewRef);
            else
                Ed.currentEffect.Tree.Add(NewRef);
            if (NewRef is EmitterReference em)
                Ed.currentEffect.Emitters.Add(em);
            if (NewRef is AppearanceReference ap)
                Ed.currentEffect.Appearances.Add(ap);
            Ed.OnTreeChange();
        }

        public override void Undo()
        {
            if(Parent != null)
                Parent.Children.Remove(NewRef);
            else
                Ed.currentEffect.Tree.Remove(NewRef);
            if (NewRef is EmitterReference em)
                Ed.currentEffect.Emitters.Remove(em);
            if (NewRef is AppearanceReference ap)
                Ed.currentEffect.Appearances.Remove(ap);
            Ed.OnTreeChange();
        }
    }

    static IEnumerable<NodeReference> GetEffectNodes(ParticleEffect effect)
    {
        foreach (var t in effect.Tree)
        {
            foreach (var n in WalkTree(t))
                yield return n;
        }
    }

    static IEnumerable<NodeReference> WalkTree(NodeReference current)
    {
        foreach (var x in current.Children)
        {
            foreach (var y in WalkTree(x))
            {
                yield return y;
            }
        }
        yield return current;
    }

    static (EditorAction action, int refCount) DeleteNodeAction(AleEditor ed, FxNode node)
    {
        List<EditorAction> actions = new();
        HashSet<NodeReference> removed = new();
        foreach (var fx in ed.ParticleFile.Effects)
        {
            foreach (var n in GetEffectNodes(fx))
            {
                if (removed.Contains(n))
                    continue;
                if (n.Node == node)
                {
                    actions.Add(DeleteReferenceAction(ed, n, fx));
                    removed.Add(n);
                }
            }
        }
        actions.Add(new NodeRemove(ed.ParticleFile.Nodes, node, ed));
        return (EditorAggregateAction.Create(actions.ToArray()), removed.Count);
    }


    static EditorAction DeleteReferenceAction(AleEditor ed, NodeReference n, ParticleEffect effect)
    {
        List<EditorAction> actions = new();
        var toRemove = WalkTree(n).ToList();
        foreach (var node in toRemove)
        {
            if (node is AppearanceReference ap)
            {
                foreach (var emit in GetEffectNodes(effect)
                             .Where(x => !toRemove.Contains(x))
                             .OfType<EmitterReference>())
                {
                    if (emit.Linked == ap)
                    {
                        actions.Add(EditorPropertyModification<AppearanceReference>.Create(
                            "Linked", () => ref emit.Linked, ap, null, null)
                        );
                    }
                }
            }
            else if (node is FieldReference fl)
            {
                foreach (var emit in GetEffectNodes(effect)
                             .Where(x => !toRemove.Contains(x))
                             .OfType<AppearanceReference>())
                {
                    if (emit.Linked == fl)
                    {
                        actions.Add(EditorPropertyModification<FieldReference>.Create(
                            "Linked", () => ref emit.Linked, fl, null, null)
                        );
                    }
                }
            }
            actions.Add(new RemoveNodeReference(ed, node));
        }
        return EditorAggregateAction.Create(actions.ToArray());
    }

    class RemoveNodeReference(AleEditor Ed, NodeReference Node) : EditorAction
    {
        private int pIndex = -1;

        public override void Commit()
        {
            // Clear references
            if (Node is EmitterReference em)
            {
                Ed.currentEffect.Emitters.Remove(em);
            }
            if (Node is AppearanceReference ap)
            {
                Ed.currentEffect.Appearances.Remove(ap);
            }
            // Remove node
            if (Node.Parent != null)
            {
                pIndex = Node.Parent.Children.IndexOf(Node);
                Node.Parent.Children.Remove(Node);
            }
            else
            {
                pIndex = Ed.currentEffect.Tree.IndexOf(Node);
                Ed.currentEffect.Tree.Remove(Node);
            }
            Ed.OnTreeChange();
        }

        public override void Undo()
        {
            if (Node is EmitterReference em)
            {
                Ed.currentEffect.Emitters.Add(em);
            }
            if (Node is AppearanceReference ap)
            {
                Ed.currentEffect.Appearances.Add(ap);
            }
            if(Node.Parent != null)
                Node.Parent.Children.Insert(pIndex, Node);
            else
                Ed.currentEffect.Tree.Insert(pIndex, Node);
            Ed.OnTreeChange();
        }
    }

    class NodeCreate(Dictionary<string, FxNode> Collection, FxNode Value, AleEditor Tab)
        : EditorAction
    {
        public override void Commit()
        {
            Collection.Add(Value.NodeName, Value);
            Tab.RefreshNodeList();
        }

        public override void Undo()
        {
            Collection.Remove(Value.NodeName);
            Tab.RefreshNodeList();
        }

        public override string ToString() => $"Add Node: {Value.NodeName}";
    }

    class NodeRemove(Dictionary<string, FxNode> Collection, FxNode Value, AleEditor Tab)
        : EditorAction
    {

        public override void Commit()
        {
            Collection.Remove(Value.NodeName);
            Tab.RefreshNodeList();
        }

        public override void Undo()
        {
            Collection.Add(Value.NodeName, Value);
            Tab.RefreshNodeList();
        }

        public override string ToString() => $"Remove Node: {Value.NodeName}";
    }
}
