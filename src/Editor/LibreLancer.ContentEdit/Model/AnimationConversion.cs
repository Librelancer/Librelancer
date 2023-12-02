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
}
