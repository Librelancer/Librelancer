using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Resources;
using LibreLancer.Data;
using LibreLancer.Render;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Dfm;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class DfmExporter
{
    static ushort[] TriangleStripToList(ReadOnlySpan<ushort> strip)
    {
        if (strip.Length < 3)
            return Array.Empty<ushort>();

        var result = new ushort[(strip.Length - 2) * 3];
        int dst = 0;

        for (int i = 0; i < strip.Length - 2; i++)
        {
            var a = strip[i];
            var b = strip[i + 1];
            var c = strip[i + 2];

            if (a == b || b == c || a == c)
                continue;

            if ((i & 1) == 0)
            {
                result[dst++] = a;
                result[dst++] = b;
                result[dst++] = c;
            }
            else
            {
                result[dst++] = b;
                result[dst++] = a;
                result[dst++] = c;
            }
        }

        if (dst != result.Length)
            Array.Resize(ref result, dst);

        return result;
    }



    static Animation ExportAnimation(DfmFile dfm, Script script)
    {
        var skinning = new DfmSkinning(dfm);

        var translations = new List<TranslationChannel>();
        var rotations = new List<RotationChannel>();
        var duration = script.CalculateDuration();

        int frameCount = (int)(duration / (1 / 60.0f));
        if (frameCount < 1)
            frameCount = 1;

        var channels = new (TranslationChannel T, RotationChannel R)[skinning.Instances.Length];
        for (int i = 0; i < skinning.Instances.Length; i++)
        {
            if (skinning.Instances[i] == null)
                continue;
            var tr = new TranslationChannel()
            {
                Target = skinning.Instances[i].Name,
                Keyframes = new TranslationKeyframe[frameCount]
            };
            var rot = new RotationChannel()
            {
                Target = skinning.Instances[i].Name,
                Keyframes = new RotationKeyframe[frameCount]
            };
            channels[i] = (tr, rot);
            translations.Add(tr);
            rotations.Add(rot);
        }

        int[] cursors = new int[skinning.Instances.Length];
        int[] maps = new int[skinning.Instances.Length];
        for (int i = 0; i < maps.Length; i++)
            maps[i] = -1;

        for (int i = 0; i < script.JointMaps.Count; i++)
        {
            ref var joint = ref script.JointMaps[i];
            if (!skinning.Bones.TryGetValue(joint.ChildName, out var b))
                continue;
            maps[Array.IndexOf(skinning.Instances, b)] = i;
        }

        for (int i = 0; i < frameCount; i++)
        {
            float t = i * duration <= 0 ? 0 : (duration / (frameCount - 1));

            for (int j = 0; j < skinning.Instances.Length; j++)
            {
                if (skinning.Instances[j] == null)
                    continue;
                if (maps[j] != -1)
                {
                    ref var joint = ref script.JointMaps[maps[j]];
                    ref var ch = ref joint.Channel;
                    if (ch.HasOrientation)
                        skinning.Instances[j].Rotation = ch.QuaternionAtTime(t, ref cursors[j]);
                    if (ch.HasPosition)
                        skinning.Instances[j].Translation = ch.PositionAtTime(t, ref cursors[j]);
                }
                channels[j].T.Keyframes[i] = new(
                    t, skinning.Instances[j].Translation + skinning.Instances[j].Origin
                );
                channels[j].R.Keyframes[i] = new(
                    t, Quaternion.Concatenate(skinning.Instances[j].OriginalRotation,
                        skinning.Instances[j].Rotation)
                );
            }
        }

        return new()
        {
            Name = script.Name,
            Translations = translations.Select(AnimationConversion.Resample).ToArray(),
            Rotations = rotations.Select(AnimationConversion.Resample).ToArray()
        };
    }

    public static SimpleMesh.Model Export(DfmFile dfm, IEnumerable<Script> anm, ResourceManager resources)
    {
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};

        var mesh = dfm.Levels[0];
        var a = VertexAttributes.Normal | VertexAttributes.Texture1 | VertexAttributes.Joints;
        if (mesh.UV1Indices != null &&
            mesh.UV1 != null)
        {
            a |= VertexAttributes.Texture2;
        }
        var va = new VertexArray(a, mesh.PointIndices.Length);
        for (int i = 0; i < mesh.PointIndices.Length; i++)
        {
            var first = mesh.PointBoneFirst[mesh.PointIndices[i]];
            var count = mesh.PointBoneCount[mesh.PointIndices[i]];
            int id1 = 0, id2 = 0, id3 = 0, id4 = 0;
            var weights = new Vector4(1, 0, 0, 0);

            if (count > 0)
            {
                id1 = mesh.BoneIdChain[first];
                weights.X = mesh.BoneWeightChain[first];
            }

            if (count > 1)
            {
                id2 = mesh.BoneIdChain[first + 1];
                weights.Y = mesh.BoneWeightChain[first + 1];
            }

            if (count > 2)
            {
                id3 = mesh.BoneIdChain[first + 2];
                weights.Z = mesh.BoneWeightChain[first + 2];
            }

            if (count > 3)
            {
                id4 = mesh.BoneIdChain[first + 3];
                weights.W = mesh.BoneWeightChain[first + 3];
            }

            va.Position[i] = mesh.Points[mesh.PointIndices[i]];
            va.Normal[i] = mesh.VertexNormals[mesh.PointIndices[i]];
            va.Texture1[i] = mesh.UV0[mesh.UV0Indices[i]];
            if (mesh.UV1 != null && mesh.UV1Indices != null)
            {
                va.Texture2[i] = mesh.UV1[mesh.UV1Indices[i]];
            }
            va.JointIndices[i] = new((ushort)id1, (ushort)id2, (ushort)id3, (ushort)id4);
            va.JointWeights[i] = weights;
        }

        var tg = new List<TriangleGroup>();
        List<ushort> indices = new();
        foreach (var fg in mesh.FaceGroups)
        {
            var crc = CrcTool.FLModelCrc(fg.MaterialName);
            var start = indices.Count;
            indices.AddRange(TriangleStripToList(fg.TriangleStripIndices));
            tg.Add(new TriangleGroup(MaterialExporter.GetMaterial(crc, resources, output.Materials))
            {
                StartIndex = start,
                IndexCount =  indices.Count - start
            });
        }

        var g = new Geometry(va, new Indices(indices.ToArray()));
        g.Name = "dfm.mesh";
        g.Groups = tg.ToArray();

        var attachment = new ModelNode() { Name = "$Attachment" };

        var length = (dfm.Parts.Keys.Max() + 1);

        ModelNode[] bones = new ModelNode[length];
        Matrix4x4[] inverseBindMatrices = new Matrix4x4[length];

        for(int i = 0; i < inverseBindMatrices.Length; i++)
            inverseBindMatrices[i] = Matrix4x4.Identity;

        Dictionary<string, ModelNode> bonesByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in dfm.Parts)
        {

            Matrix4x4.Invert(kv.Value.Bone.BoneToRoot, out inverseBindMatrices[kv.Key]);
            var b = new ModelNode()
            {
                Name = kv.Value.objectName
            };
            bones[kv.Key] = b;
            bonesByName[b.Name] = b;
        }

        HashSet<ModelNode> withParents = new();

        foreach (var con in dfm.Constructs.Constructs)
        {
            if (!bonesByName.ContainsKey(con.ChildName))
            {
                continue;
            }

            var inst = bonesByName[con.ChildName];

            if (!string.IsNullOrEmpty(con.ParentName))
            {
                var parent = bonesByName[con.ParentName];
                parent.Children.Add(inst);
                withParents.Add(inst);
            }

            inst.Transform = con.Rotation * Matrix4x4.CreateTranslation(con.Origin);
        }

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                bones[i] = attachment;
        }

        var root = new ModelNode() { Name = "MESH", Geometry = g };
        root.Children.Add(attachment);

        foreach (var b in bonesByName.Values)
        {
            if (!withParents.Contains(b))
            {
                root.Children.Add(b);
            }
        }


        var anims = new List<Animation>();
        if (anm != null)
        {
            foreach (var sc in anm)
            {
                anims.Add(ExportAnimation(dfm, sc));
            }
        }


        var sk = new Skin() { Bones = bones, InverseBindMatrices = inverseBindMatrices, Name = "SKIN" };
        root.Skin = sk;

        output.Roots = [root];
        output.Geometries = [g];
        output.Skins = [sk];
        output.Animations = anims.ToArray();
        output.Images = MaterialExporter.ExportImages(resources, output.Materials);
        return output;
    }
}
