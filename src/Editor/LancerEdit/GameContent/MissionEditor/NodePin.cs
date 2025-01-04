using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor;

public class NodePin
{
    private static List<NodeLink> AllLinks = [];

    public PinId Id { get; }
    public Node OwnerNode { get; }
    public LinkType LinkType { get; }
    public PinKind PinKind { get; }
    public List<NodeLink> Links { get; } = [];
    public int LinkCapacity { get; }

    public NodePin(int id, Node owner, LinkType type, PinKind kind, int linkCapacity = int.MaxValue - 1)
    {
        Id = id;
        OwnerNode = owner;
        LinkType = type;
        PinKind = kind;
        LinkCapacity = linkCapacity;
    }

    public NodeLink CreateLink(ref int nextId, [NotNull] NodePin endPin, Action<string, bool> labelCallback)
    {
        if (endPin == this)
        {
            NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            return null;
        }

        if (endPin.PinKind == PinKind)
        {
            labelCallback("x Incompatible Pin Kind", false);
            NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            return null;
        }

        if (endPin.OwnerNode == OwnerNode)
        {
            labelCallback("x Cannot connect to self", false);
            NodeEditor.RejectNewItem(Color4.Red, 2.0f);
            return null;
        }

        if (endPin.LinkType != LinkType)
        {
            labelCallback("x Incompatible Link Type", false);
            NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            return null;
        }

        if (Links.Count + 1 >= LinkCapacity ||
            endPin.Links.Count + 1 >= endPin.LinkCapacity)
        {
            labelCallback("x Pin has reached link capacity", false);
            NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            return null;
        }

        if (AllLinks.Any(x => x.StartPin == this && x.EndPin == endPin))
        {
            labelCallback("x Link already exists", false);
            NodeEditor.RejectNewItem(new Color4(255, 128, 128, 255));
            return null;
        }

        labelCallback("+ Create Link", true);
        if (!NodeEditor.AcceptNewItem(new Color4(128, 255, 128, 255), 4.0f))
        {
            return null;
        }

        var nodeLink = new NodeLink(nextId++, this, endPin)
        {
            Color = endPin.OwnerNode.Color
        };

        Links.Add(nodeLink);
        endPin.Links.Add(nodeLink);
        AllLinks.Add(nodeLink);
        return nodeLink;
    }
}
