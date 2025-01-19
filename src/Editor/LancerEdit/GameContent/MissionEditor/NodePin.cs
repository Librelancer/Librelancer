using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor;

public class NodePin(Node owner, LinkType type, PinKind kind, int linkCapacity = int.MaxValue - 1)
{
    private static readonly List<NodeLink> _allLinks = [];
    public static ReadOnlyCollection<NodeLink> AllLinks => _allLinks.AsReadOnly();

    public PinId Id { get; } = NodeEditorId.Next();
    public Node OwnerNode { get; } = owner;
    public LinkType LinkType { get; } = type;
    public PinKind PinKind { get; } = kind;
    public List<NodeLink> Links { get; } = [];
    public int LinkCapacity { get; } = linkCapacity;

    [SuppressMessage("ReSharper", "InvertIf")]
    public NodeLink CreateLink(ref int nextId, [NotNull] NodePin endPin, Action<string, bool> labelCallback)
    {
        if (endPin == this)
        {
            if (labelCallback != null)
            {
                NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            }

            return null;
        }

        if (endPin.PinKind == PinKind)
        {
            if (labelCallback != null)
            {
                labelCallback("x Incompatible Pin Kind", false);
                NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            }


            return null;
        }

        if (endPin.OwnerNode == OwnerNode)
        {
            if (labelCallback != null)
            {
                labelCallback("x Cannot connect to self", false);
                NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            }

            return null;
        }

        if (endPin.LinkType != LinkType)
        {
            if (labelCallback != null)
            {
                labelCallback("x Incompatible Link Type", false);
                NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            }


            return null;
        }

        if (Links.Count + 1 > LinkCapacity ||
            endPin.Links.Count + 1 > endPin.LinkCapacity)
        {
            if (labelCallback != null)
            {
                labelCallback("x Pin has reached link capacity", false);
                NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            }

            return null;
        }

        if (_allLinks.Any(x => x.StartPin == this && x.EndPin == endPin))
        {
            if (labelCallback != null)
            {
                labelCallback("x Link already exists", false);
                NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            }

            return null;
        }

        if (labelCallback != null)
        {
            NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            labelCallback("+ Create Link", true);
            if (!NodeEditor.AcceptNewItem(new Color4(128, 255, 128, 255), 4.0f))
            {
                return null;
            }
        }

        var nodeLink = new NodeLink(this, endPin)
        {
            Color = endPin.OwnerNode.Color
        };

        Links.Add(nodeLink);
        endPin.Links.Add(nodeLink);
        _allLinks.Add(nodeLink);
        return nodeLink;
    }

    public static void DeleteLink(LinkId id)
    {
        var link = _allLinks.Find(x => x.Id == id);
        if (link == null)
        {
            return;
        }

        link.StartPin.OwnerNode.OnLinkRemoved(link);
        link.EndPin.OwnerNode.OnLinkRemoved(link);

        _allLinks.Remove(link);
        link.StartPin.Links.Remove(link);
        link.EndPin.Links.Remove(link);
    }
}
