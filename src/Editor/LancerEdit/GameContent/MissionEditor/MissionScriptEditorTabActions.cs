using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor;

partial class MissionScriptEditorTab
{
    class NewTriggerAction(Vector2 position, MissionScriptEditorTab tab) : EditorAction
    {
        private NodeMissionTrigger trigger = new(null, tab);
        public override void Commit()
        {
            tab.nodes.Add(trigger);
            tab.nodeRelocationQueue.Enqueue((trigger.Id, position));
            tab.SetupJumpList();
        }

        public override void Undo()
        {
            tab.nodes.Remove(trigger);
            tab.SetupJumpList();
        }

        public override string ToString() => "New Trigger";
    }

    class RenameTriggerAction(
        NodeMissionTrigger trigger,
        MissionScriptEditorTab tab,
        string old,
        string updated) : EditorModification<string>(old, updated)
    {
        public override void Set(string value)
        {
            trigger.Data.Nickname = value;
            tab.SetupJumpList();
        }
    }

    class NewCommentAction(Vector2 position, MissionScriptEditorTab tab) : EditorAction
    {
        private CommentNode node = new();
        public override void Commit()
        {
            tab.nodes.Add(node);
            tab.nodeRelocationQueue.Enqueue((node.Id, position));
        }

        public override void Undo()
        {
            tab.nodes.Remove(node);
        }

        public override string ToString() => "New Comment";
    }

    class CreateLinkAction(NodePin startPin, NodePin endPin, MissionScriptEditorTab tab) : EditorAction
    {
        private NodeLink created;
        public override void Commit()
        {
            created = startPin.CreateLink(endPin, null);
            if (created is not null)
            {
                foreach (var node in tab.nodes)
                {
                    node.OnLinkCreated(created);
                }
            }
        }

        public override void Undo()
        {
            if (created is null)
                return;
            NodePin.DeleteLink(created.Id);
            created = null;
        }

        public override string ToString() => "Link Nodes";
    }

    class DeleteLinkAction(NodeLink link) : EditorAction
    {
        public override void Commit()
        {
            NodePin.DeleteLink(link.Id);
        }

        public override void Undo()
        {
            NodePin.InsertLink(link);
        }

        public override string ToString() => "Unlink Nodes";
    }

    class MoveNodeAction(NodeId Id, Vector2 Old, Vector2 New, MissionScriptEditorTab Tab) : EditorAction
    {
        public override void Commit() => Tab.nodeRelocationQueue.Enqueue((Id, New));
        public override void Undo() => Tab.nodeRelocationQueue.Enqueue((Id, Old));
        public override string ToString() => $"Move Node {Id}: {Old} -> {New}";
    }

    class SetNodeGroupSize(NodeId Id, Vector2 Old, Vector2 New, MissionScriptEditorTab Tab)
        : EditorModification<Vector2>(Old, New)
    {
        public override void Set(Vector2 value)
        {
            var node = Tab.nodes.OfType<CommentNode>().FirstOrDefault(x => x.Id == Id);
            if (node == null)
                return;
            node.SetGroupSize(value);
        }
        public override string ToString() => $"Resize {Id} {Old} -> {New}";
    }

    class SetNpcShipFileAction(string old, string updated, MissionScriptEditorTab tab)
        : EditorModification<string>(old, updated)
    {
        public override void Set(string value)
        {
            tab.missionIni.Info.NpcShipFile = value;
            tab.missionIni.ShipIni = new NPCShipIni(tab.missionIni.Info.NpcShipFile, tab.gameData.GameData.VFS);
        }
    }

    class DeleteActionAction(NodeMissionTrigger node, int index, MissionScriptEditorTab tab) : EditorAction
    {
        private List<(Node Start, Node End, LinkType LinkType, LinkId LinkId)> savedLinks;
        private NodeTriggerEntry action;
        public override void Commit()
        {
            action = node.Actions[index];
            savedLinks = tab.GetLinks(action);
            node.Actions.RemoveAt(index);
            foreach (var l in savedLinks)
            {
                NodePin.DeleteLink(l.LinkId);
            }
        }

        public override void Undo()
        {
            node.Actions.Insert(index, action);
            List<NodeLink> newLinks = new();
            foreach (var l in savedLinks)
            {
                if(tab.TryLinkNodes(l.Start, l.End, l.LinkType, out var link))
                    newLinks.Add(link);
            }
            foreach (var n in newLinks)
            {
                n.StartPin.OwnerNode.OnLinkCreated(n);
                n.EndPin.OwnerNode.OnLinkCreated(n);
            }
        }

        public override string ToString() => "Delete Action";
    }

    class DeleteConditionAction(NodeMissionTrigger node, int index, MissionScriptEditorTab tab) : EditorAction
    {
        private List<(Node Start, Node End, LinkType LinkType, LinkId LinkId)> savedLinks;
        private NodeTriggerEntry condition;
        public override void Commit()
        {
            condition = node.Conditions[index];
            savedLinks = tab.GetLinks(condition);
            node.Conditions.RemoveAt(index);
            foreach (var l in savedLinks)
            {
                NodePin.DeleteLink(l.LinkId);
            }
        }

        public override void Undo()
        {
            node.Conditions.Insert(index, condition);
            List<NodeLink> newLinks = new();
            foreach (var l in savedLinks)
            {
                if(tab.TryLinkNodes(l.Start, l.End, l.LinkType, out var link))
                    newLinks.Add(link);
            }
            foreach (var n in newLinks)
            {
                n.StartPin.OwnerNode.OnLinkCreated(n);
                n.EndPin.OwnerNode.OnLinkCreated(n);
            }
        }

        public override string ToString() => "Delete Condition";
    }

    class DeleteNodeAction(NodeId id, Node node, MissionScriptEditorTab tab) : EditorAction
    {
        private List<(Node Start, Node End, LinkType LinkType, LinkId LinkId)> savedLinks;
        public override void Commit()
        {
            savedLinks = tab.GetLinks(node);
            tab.nodeEditActions.Enqueue(() => NodeEditor.DeleteNode(id));
            tab.nodes.Remove(node);
            foreach (var l in savedLinks)
            {
                NodePin.DeleteLink(l.LinkId);
            }
            tab.SetupJumpList();
        }

        public override void Undo()
        {
            tab.nodes.Add(node);
            List<NodeLink> newLinks = new();
            foreach (var l in savedLinks)
            {
                if(tab.TryLinkNodes(l.Start, l.End, l.LinkType, out var link))
                    newLinks.Add(link);
            }
            foreach (var n in newLinks)
            {
                n.StartPin.OwnerNode.OnLinkCreated(n);
                n.EndPin.OwnerNode.OnLinkCreated(n);
            }
            tab.SetupJumpList();
        }

        public override string ToString() => $"Delete Node {id}";
    }
}
