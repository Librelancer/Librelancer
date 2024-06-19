using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;

namespace LibreLancer.ContentEdit.Model;

public class ModelHpNode
{
    public string Name;
    public LUtfNode Node;
    public LUtfNode HardpointsNode;

    public void HardpointsToNodes(List<Hardpoint> hardpoints)
    {
        if (hardpoints.Count == 0)
        {
            if (HardpointsNode != null)
            {
                Node.Children.Remove(HardpointsNode);
                HardpointsNode = null;
            }

            return;
        }

        if (HardpointsNode == null)
        {
            HardpointsNode = new LUtfNode() {Name = "Hardpoints", Parent = Node};
            Node.Children.Add(HardpointsNode);
        }

        HardpointsNode.Children = new List<LUtfNode>();
        LUtfNode fix = null;
        LUtfNode rev = null;
        var hps = hardpoints.Select(x => x.Definition);
        if (hps.Any((x) => x is FixedHardpointDefinition))
        {
            fix = new LUtfNode() {Name = "Fixed", Parent = HardpointsNode};
            fix.Children = new List<LUtfNode>();
            HardpointsNode.Children.Add(fix);
        }

        if (hps.Any((x) => x is RevoluteHardpointDefinition))
        {
            rev = new LUtfNode() {Name = "Revolute", Parent = HardpointsNode};
            rev.Children = new List<LUtfNode>();
            HardpointsNode.Children.Add(rev);
        }

        foreach (var hp in hps)
        {
            var node = new LUtfNode() {Name = hp.Name, Children = new List<LUtfNode>()};
            node.Children.Add(new LUtfNode()
            {
                Name = "Orientation", Parent = node,
                Data = UnsafeHelpers.CastArray(new float[]
                {
                    hp.Orientation.M11, hp.Orientation.M21, hp.Orientation.M31,
                    hp.Orientation.M12, hp.Orientation.M22, hp.Orientation.M32,
                    hp.Orientation.M13, hp.Orientation.M23, hp.Orientation.M33
                })
            });
            node.Children.Add(new LUtfNode()
            {
                Name = "Position", Parent = node,
                Data = UnsafeHelpers.CastArray(new float[] {hp.Position.X, hp.Position.Y, hp.Position.Z})
            });
            if (hp is FixedHardpointDefinition)
            {
                node.Parent = fix;
                fix.Children.Add(node);
            }
            else
            {
                var revolute = (RevoluteHardpointDefinition) hp;
                node.Children.Add(new LUtfNode()
                {
                    Name = "Axis", Parent = node,
                    Data = UnsafeHelpers.CastArray(new float[]
                    {
                        revolute.Axis.X, revolute.Axis.Y, revolute.Axis.Z
                    })
                });
                node.Children.Add(new LUtfNode()
                {
                    Name = "Min", Parent = node,
                    Data = BitConverter.GetBytes(revolute.Min)
                });
                node.Children.Add(new LUtfNode()
                {
                    Name = "Max", Parent = node,
                    Data = BitConverter.GetBytes(revolute.Max)
                });
                node.Parent = rev;
                rev.Children.Add(node);
            }
        }
    }
}
