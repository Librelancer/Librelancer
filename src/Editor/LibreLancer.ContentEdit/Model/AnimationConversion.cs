using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SM = SimpleMesh;
namespace LibreLancer.ContentEdit.Model;

static class AnimationConversion
{
    public static SM.Animation ExportAnimation(CmpFile cmp, Script script)
    {
        var anm = new SM.Animation();
        anm.Name = script.Name;
        var translations = new List<SM.TranslationChannel>();
        var rotations = new List<SM.RotationChannel>();
        foreach (var map in script.JointMaps)
        {
            //Get target + construct
            var tgt = map.ChildName;
            var tgtNode = cmp.Parts.FirstOrDefault(x => x.ObjectName.Equals(tgt, StringComparison.OrdinalIgnoreCase));
            if (tgtNode == null)
                continue;
            var con = tgtNode.Construct;
            if (con == null)
                continue;
            // Only export rev
            if (con is RevConstruct rev)
            {
                if (!map.Channel.HasAngle)
                    continue;
                var rot = new SM.RotationChannel();
                var cloned = rev.Clone();
                rot.Target = tgt;
                rot.Keyframes = new SM.RotationKeyframe[map.Channel.FrameCount];
                for (int i = 0; i < rot.Keyframes.Length; i++)
                {
                    if (map.Channel.Interval < 0)
                        rot.Keyframes[i].Time = map.Channel.GetTime(i);
                    else
                        rot.Keyframes[i].Time = map.Channel.Interval * i;
                    cloned.Update(map.Channel.GetAngle(i), Quaternion.Identity);
                    rot.Keyframes[i].Rotation = cloned.LocalTransform.ExtractRotation();
                }
                rotations.Add(rot);
            }
        }
        anm.Translations = translations.ToArray();
        anm.Rotations = rotations.ToArray();
        return anm;
    }

    static LUtfNode ConstructChannel(NodeWriter frames, int len, float interval, int type)
    {
        LUtfNode channel = new LUtfNode() {Name = "Channel", Children = new List<LUtfNode>()};
        using var header = new NodeWriter();
        header.Write((int)len);
        header.Write(interval); //embedded times
        header.Write(type); //angles
        channel.Children.Add(header.GetUtfNode("Header", channel));
        channel.Children.Add(frames.GetUtfNode("Frames", channel));
        return channel;
    }

    static float CalculateInterval(SM.RotationChannel rots)
    {
        if (rots.Keyframes.Length < 2)
            return -1;
        if (rots.Keyframes[0].Time != 0)
            return -1;
        var interval = rots.Keyframes[1].Time;
        for (int i = 1; i < rots.Keyframes.Length; i++)
        {
            if (Math.Abs(rots.Keyframes[i].Time - (interval) * i) > 0.001f)
                return -1.0f;
        }
        return interval;
    }

    record struct RevProps(Vector3 axis, float min, float max);

    static (LUtfNode ch, RevProps props) QuatsToAngleChannel(SM.RotationChannel rots, Matrix4x4 target)
    {
        //Make relative to construct, imported animations only store final rotations.
        var invRotate = target.ExtractRotation();
        invRotate = Quaternion.Inverse(invRotate);
        Quaternion[] transformed = new Quaternion[rots.Keyframes.Length];
        for (int i = 0; i < rots.Keyframes.Length; i++)
        {
            transformed[i] = rots.Keyframes[i].Rotation * invRotate;
        }
        //At angle = 0, the axis is arbitrary (doesn't matter)
        Vector3[] axis = new Vector3[rots.Keyframes.Length];
        float[] angles = new float[rots.Keyframes.Length];

        float[] outputAngles = new float[rots.Keyframes.Length];

        for (int i = 0; i < rots.Keyframes.Length; i++)
        {
            //extract angles
            BepuUtilities.QuaternionEx.GetAxisAngleFromQuaternion(transformed[i], out axis[i], out angles[i]);
        }

        Vector3? rotationAxis = null;
        for (int i = 0; i < rots.Keyframes.Length; i++)
        {
            if (Math.Abs(angles[i]) < float.Epsilon) {
                outputAngles[i] = 0;
            }
            else {
                if (rotationAxis != null)
                {
                    var diff = Vector3.Distance(axis[i], rotationAxis.Value);
                    if (diff > 0.01f)
                    {
                        //Axis flipped, can flip the angle
                        //Otherwise this doesn't all follow one axis of rotation.
                        if (Vector3.Distance(-axis[i], rotationAxis.Value) < 0.01f)
                            outputAngles[i] = -angles[i];
                        else
                            return (null, default);
                    }
                    else
                    {
                        outputAngles[i] = angles[i];
                    }
                }
                else
                {
                    rotationAxis = axis[i];
                    outputAngles[i] = angles[i];
                }
            }
        }
        var interval = CalculateInterval(rots);
        var frames = new NodeWriter();
        for (int i = 0; i < rots.Keyframes.Length; i++)
        {
            if(interval < 0)
                frames.Write(rots.Keyframes[i].Time);
            frames.Write(outputAngles[i]);
        }
        return (ConstructChannel(frames, rots.Keyframes.Length, interval, 0x1),
            new RevProps(rotationAxis ?? Vector3.Zero, outputAngles.Min(), outputAngles.Max()));
    }

    public static EditResult<LUtfNode> ImportAnimation(List<ImportedModelNode> allNodes, SM.Animation anim)
    {
        var n = new LUtfNode() {Name = anim.Name};
        n.Children = new List<LUtfNode>();
        int i = 0;
        var messages = new List<EditMessage>();
        foreach (var rot in anim.Rotations)
        {
            var child = allNodes.FirstOrDefault(x =>
                x.Name.Equals(rot.Target, StringComparison.OrdinalIgnoreCase));
            if (child == null)
            {
                messages.Add(EditMessage.Warning($"'{anim.Name}' skipping unknown target '{rot.Target}'"));
                continue;
            }
            if (child.Construct is RevConstruct rev)
            {
                var jm = new LUtfNode() {Parent = n, Name = "Joint map " + i, Children = new List<LUtfNode>()};
                jm.Children.Add(new LUtfNode()
                {
                    Name = "Child Name",
                    StringData = child.Name,
                    Parent = jm,
                });
                jm.Children.Add(new LUtfNode()
                {
                    Name = "Parent Name",
                    StringData = child.Construct.ParentName,
                    Parent = jm
                });
                var (ch, props) = QuatsToAngleChannel(rot, child.Construct.LocalTransform);

                if (ch == null)
                {
                    messages.Add(EditMessage.Warning($"Rotation in '{anim.Name}' could not map to axis/angle"));
                }
                else
                {
                    if (!child.ConstructPropertiesSet)
                    {
                        rev.Min = props.min;
                        rev.Max = props.max;
                        rev.AxisRotation = props.axis;
                        child.ConstructPropertiesSet = true;
                    }
                    else
                    {
                        if(props.min < rev.Min)
                            messages.Add(EditMessage.Warning($"Rotation in '{anim.Name} 'goes below target '{rot.Target}' min ({props.min} < {rev.Min})"));
                        if(props.max > rev.Max)
                            messages.Add(EditMessage.Warning($"Rotation in '{anim.Name}' goes above target '{rot.Target}' max ({props.max} > {rev.Max})"));
                        if(Vector3.Distance(props.axis, rev.AxisRotation) > 0.1f)
                            messages.Add(EditMessage.Warning($"Rotation in '{anim.Name}' does not follow target '{rot.Target}' rotation axis"));
                    }
                    ch.Parent = ch;
                    jm.Children.Add(ch);
                    n.Children.Add(jm);
                    i++;
                }
            }
            else
            {
                messages.Add(EditMessage.Warning($"'{anim.Name}' skipping target not set as rev '{rot.Target}'"));
            }
        }
        if (i == 0)
            return EditResult<LUtfNode>.Error($"Could not convert animation '{anim.Name}'", messages);
        return new EditResult<LUtfNode>(n, messages);
    }
}
