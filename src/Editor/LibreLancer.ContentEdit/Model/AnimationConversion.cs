using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;
using SM = SimpleMesh;
namespace LibreLancer.ContentEdit.Model;

static class AnimationConversion
{
    public static SM.Animation DefaultAnimation(CmpFile cmp)
    {
        var anm = new SM.Animation();
        anm.Name = "<Default>";
        var translations = new List<SM.TranslationChannel>();
        var rotations = new List<SM.RotationChannel>();
        foreach (var p in cmp.Parts.Where(x => x.Construct != null))
        {
            var con = p.Construct.Clone();
            var tr = new SM.TranslationChannel();
            tr.Target = p.ObjectName;
            tr.Keyframes = [
                new (0, con.Origin)
            ];
            translations.Add(tr);
            var rot = new SM.RotationChannel();
            rot.Target = p.ObjectName;
            rot.Keyframes = [
                new SM.RotationKeyframe(0, Quaternion.Normalize(con.Rotation))
            ];
            rotations.Add(rot);
        }
        anm.Translations = translations.ToArray();
        anm.Rotations = rotations.ToArray();
        return anm;
    }

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
            // Only export rev/pris
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
                    rot.Keyframes[i].Rotation = Quaternion.Normalize(cloned.LocalTransform.Orientation);
                }
                rotations.Add(rot);
            }
            else if (con is PrisConstruct pris)
            {
                if (!map.Channel.HasAngle)
                    continue;
                var tr = new SM.TranslationChannel();
                var cloned = pris.Clone();
                tr.Target = tgt;
                tr.Keyframes = new SM.TranslationKeyframe[map.Channel.FrameCount];
                for (int i = 0; i < tr.Keyframes.Length; i++)
                {
                    if (map.Channel.Interval < 0)
                        tr.Keyframes[i].Time = map.Channel.GetTime(i);
                    else
                        tr.Keyframes[i].Time = map.Channel.Interval * i;
                    cloned.Update(map.Channel.GetAngle(i), Quaternion.Identity);
                    tr.Keyframes[i].Translation = cloned.LocalTransform.Position;
                }
                translations.Add(tr);
            }
        }
        anm.Translations = translations.ToArray();
        anm.Rotations = rotations.ToArray();
        return anm;
    }

    static float CalculateInterval(SM.TranslationChannel trs)
    {
        if (trs.Keyframes.Length < 2)
            return -1;
        if (trs.Keyframes[0].Time != 0)
            return -1;
        var interval = trs.Keyframes[1].Time;
        for (int i = 1; i < trs.Keyframes.Length; i++)
        {
            if (Math.Abs(trs.Keyframes[i].Time - (interval) * i) > 0.001f)
                return -1.0f;
        }
        return interval;
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

    record struct PrisRevProps(Vector3 axis, float min, float max);

    static (Channel? ch, PrisRevProps) VectorsToAngleChannel(SM.TranslationChannel trs, Matrix4x4 target)
    {
        Matrix4x4.Invert(target, out var invMat);
        Vector3[] transformed = new Vector3[trs.Keyframes.Length];
        for (int i = 0; i < trs.Keyframes.Length; i++)
            transformed[i] = Vector3.Transform(trs.Keyframes[i].Translation, invMat);
        var axis = Vector3.Normalize(transformed[^1] - transformed[0]);
        float[] angles = new float[trs.Keyframes.Length];
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < transformed.Length; i++)
        {
            var d = transformed[i].Length();
            if (!ApproxEqual(axis * d, transformed[i]))
                return (null, default);
            angles[i] = d;
            min = MathF.Min(d, min);
            max = MathF.Max(d, max);
        }
        var interval = CalculateInterval(trs);
        var eb = new EditableAnmBuffer((interval < 0 ? 8 : 4) * trs.Keyframes.Length);
        var c = new Channel(0x1, trs.Keyframes.Length, interval, eb);
        for (int i = 0; i < trs.Keyframes.Length; i++)
        {
            if(interval < 0)
                eb.SetTime(ref c, i, trs.Keyframes[i].Time);
            eb.SetAngle(ref c, i, angles[i]);
        }
        return (c, new PrisRevProps(axis, min, max));
    }



    static (Channel? ch, PrisRevProps props) QuatsToAngleChannel(SM.RotationChannel rots, Matrix4x4 target)
    {
        //Make relative to construct, imported animations only store final rotations.
        var invRotate = Quaternion.Normalize(target.ExtractRotation());
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
        var eb = new EditableAnmBuffer((interval < 0 ? 8 : 4) * rots.Keyframes.Length);
        var c = new Channel(0x1, rots.Keyframes.Length, interval, eb);
        for (int i = 0; i < rots.Keyframes.Length; i++)
        {
            if(interval < 0)
                eb.SetTime(ref c, i, rots.Keyframes[i].Time);
            eb.SetAngle(ref c, i, outputAngles[i]);
        }
        return (c, new PrisRevProps(rotationAxis ?? Vector3.Zero, outputAngles.Min(), outputAngles.Max()));
    }

    static float GetAngle(Quaternion a, Quaternion b)
    {
        var dotproduct = Quaternion.Dot(a, b);
        return MathF.Acos(2 * dotproduct * dotproduct - 1);
    }

    const float TOLERANCE = 1e-4f;

    static bool ApproxEqual(Quaternion a, Quaternion b) =>
        MathF.Abs(a.X - b.X) < TOLERANCE &&
        MathF.Abs(a.Y - b.Y) < TOLERANCE &&
        MathF.Abs(a.Z - b.Z) < TOLERANCE &&
        MathF.Abs(a.W - b.W) < TOLERANCE;

    static bool ApproxEqual(Vector3 a, Vector3 b) =>
        MathF.Abs(a.X - b.X) < TOLERANCE &&
        MathF.Abs(a.Y - b.Y) < TOLERANCE &&
        MathF.Abs(a.Z - b.Z) < TOLERANCE;

    static int GetSigns(Quaternion q)
    {
        int sign = 0;
        if (BitConverter.SingleToInt32Bits(q.X) < 0)
            sign |= 0x1;
        if (BitConverter.SingleToInt32Bits(q.Y) < 0)
            sign |= 0x2;
        if (BitConverter.SingleToInt32Bits(q.Z) < 0)
            sign |= 0x4;
        return sign;
    }


    // Optimize a linear set of keyframes from a SimpleMesh rotation channel
    static SM.RotationChannel Resample(SM.RotationChannel input)
    {
        if (input.Keyframes.Length < 3)
            return input;
        var newChannel = new SM.RotationChannel();
        newChannel.Target = input.Target;
        var lastIndex = input.Keyframes.Length - 1;

        List<SM.RotationKeyframe> keyframes = new List<SM.RotationKeyframe>();
        keyframes.Add(input.Keyframes[0]);

        for (int i = 1; i < lastIndex; i++)
        {
            var timePrev = input.Keyframes[i - 1].Time;
            var time = input.Keyframes[i].Time;
            var timeNext = input.Keyframes[i + 1].Time;
            var t = (time - timePrev) / (timeNext - timePrev);

            var sample =
                Quaternion.Slerp(input.Keyframes[i - 1].Rotation, input.Keyframes[i + 1].Rotation, t);

            var angle = GetAngle(input.Keyframes[i - 1].Rotation, input.Keyframes[i].Rotation) +
                        GetAngle(input.Keyframes[i].Rotation, input.Keyframes[i + 1].Rotation);

            //Preserve when a rotation flips
            var signA = GetSigns(input.Keyframes[i].Rotation);
            var signB = GetSigns(input.Keyframes[i + 1].Rotation);
            var invSignA = (~signA) & 0x7;

            if (!ApproxEqual(sample, input.Keyframes[i].Rotation) ||
                (angle + float.Epsilon) >= MathF.PI ||
                signB == invSignA)
                keyframes.Add(input.Keyframes[i]);
        }
        keyframes.Add(input.Keyframes[^1]);
        newChannel.Keyframes = keyframes.ToArray();
        return newChannel;
    }

    static SM.TranslationChannel Resample(SM.TranslationChannel input)
    {
        if (input.Keyframes.Length < 3)
            return input;
        var newChannel = new SM.TranslationChannel();
        newChannel.Target = input.Target;
        var lastIndex = input.Keyframes.Length - 1;
        List<SM.TranslationKeyframe> keyframes = new List<SM.TranslationKeyframe>();
        keyframes.Add(input.Keyframes[0]);
        for (int i = 1; i < lastIndex; i++)
        {
            var timePrev = input.Keyframes[i - 1].Time;
            var time = input.Keyframes[i].Time;
            var timeNext = input.Keyframes[i + 1].Time;
            var t = (time - timePrev) / (timeNext - timePrev);

            var sample = Vector3.Lerp(input.Keyframes[i - 1].Translation, input.Keyframes[i + 1].Translation, t);
            if(!ApproxEqual(sample, input.Keyframes[i].Translation))
                keyframes.Add(input.Keyframes[i]);
        }
        keyframes.Add(input.Keyframes[^1]);
        newChannel.Keyframes = keyframes.ToArray();
        return newChannel;
    }

    public static EditResult<Script> ImportAnimation(List<ImportedModelNode> allNodes, SM.Animation anim)
    {
        var sc = new Script(anim.Name);
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
                var jm = new JointMap() { ChildName = child.Name, ParentName = child.Construct.ParentName };
                var resampled = Resample(rot);
                var (ch, props) = QuatsToAngleChannel(resampled, child.Construct.LocalTransform.Matrix());
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
                            messages.Add(EditMessage.Warning($"Rotation in '{anim.Name}' may not follow target '{rot.Target}' rotation axis"));
                    }

                    jm.Channel = ch.Value;
                    sc.JointMaps.Add(jm);
                }
            }
            else
            {
                messages.Add(EditMessage.Warning($"'{anim.Name}' skipping target not set as rev '{rot.Target}'"));
            }
        }
        foreach (var tr in anim.Translations)
        {
            var child = allNodes.FirstOrDefault(x =>
                x.Name.Equals(tr.Target, StringComparison.OrdinalIgnoreCase));
            if (child == null)
            {
                messages.Add(EditMessage.Warning($"'{anim.Name}' skipping unknown target '{tr.Target}'"));
                continue;
            }
            if (child.Construct is PrisConstruct pris)
            {
                var jm = new JointMap() { ChildName = child.Name, ParentName = child.Construct.ParentName };
                var resampled = Resample(tr);
                var (ch, props) = VectorsToAngleChannel(resampled, child.Construct.LocalTransform.Matrix());
                if (ch == null)
                {
                    messages.Add(EditMessage.Warning($"Translation in '{anim.Name}' could not map to single axis"));
                }
                else
                {
                    if (!child.ConstructPropertiesSet)
                    {
                        pris.Min = props.min;
                        pris.Max = props.max;
                        pris.AxisTranslation = props.axis;
                        child.ConstructPropertiesSet = true;
                    }
                    else
                    {
                        if(props.min < pris.Min)
                            messages.Add(EditMessage.Warning($"Translation in '{anim.Name} 'goes below target '{tr.Target}' min ({props.min} < {pris.Min})"));
                        if(props.max > pris.Max)
                            messages.Add(EditMessage.Warning($"Translation in '{anim.Name}' goes above target '{tr.Target}' max ({props.max} > {pris.Max})"));
                        if(Vector3.Distance(props.axis, pris.AxisTranslation) > 0.1f)
                            messages.Add(EditMessage.Warning($"Translation in '{anim.Name}' does not follow target '{tr.Target}' translation axis"));
                    }
                    jm.Channel = ch.Value;
                    sc.JointMaps.Add(jm);
                }
            }
            else
            {
                messages.Add(EditMessage.Warning($"'{anim.Name}' skipping target not set as pris '{tr.Target}'"));
            }
        }
        if (sc.JointMaps.Count == 0)
            return EditResult<Script>.Error($"Could not convert animation '{anim.Name}'", messages);
        return new EditResult<Script>(sc, messages);
    }
}
