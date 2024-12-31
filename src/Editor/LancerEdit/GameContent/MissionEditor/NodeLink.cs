using System;
using LibreLancer;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor;

/**
 * <summary>
 * The link types are named after their inputs. <para/>
 * I.e. all input pins on a Condition node should have the type of condition. <para/>
 * The output types dictate what this links to. <para/>
 * E.g. A Condition with an output of a trigger will link to a trigger element
 * </summary>
 */
[Flags]
public enum LinkType
{
    None,
    Command = 1 << 0,
    CommandList = 1 << 1,
    Trigger = 1 << 2,
    Condition = 1 << 3,
    Action = 1 << 4,
}

public class NodeLink
{
    public LinkId Id { get; }
    public NodePin StartPin  { get; set; }
    public NodePin EndPin  { get; set; }
    public VertexDiffuse Color  { get; set; }

    public NodeLink(LinkId id, NodePin startPin, NodePin endPin, VertexDiffuse? color = null)
    {
        Id = id;
        StartPin = startPin;
        EndPin = endPin;
        Color = color ?? (VertexDiffuse)Color4.White;
    }
}
