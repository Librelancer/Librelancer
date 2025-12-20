using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Data;
using LibreLancer.Sur;
using SimpleMesh;
using Xunit;

namespace LibreLancer.Tests;

public class SurfaceBuilderTests
{
    ImportedModel Load(string path)
    {
        using var stream = File.OpenRead(path);
        var mdl = Model.FromStream(stream).AutoselectRoot(out _).ApplyScale().ApplyRootTransforms(false)
            .CalculateBounds();
        var import = ImportedModel.FromSimpleMesh(path, mdl);
        Assert.True(import.IsSuccess);
        return import.Data!;
    }

    IEnumerable<uint> HullIds(SurFile sur, uint part)
    {
        HashSet<uint> seen = new HashSet<uint>();
        var p = sur.Surfaces.First(x => x.Crc == part);
        foreach (var m in p.GetHulls(false))
        {
            if (!seen.Contains(m.HullId))
            {
                yield return m.HullId;
                seen.Add(m.HullId);
            }
        }
    }

    static void AssertUnordered<T>(T[] src, params T[] expected)
    {
        Assert.Equal(expected.Length, src.Length);
        foreach (var t in expected)
            Assert.Contains(t, src);
    }

    static void AssertEpsilon(Vector3 expected, Vector3 actual)
    {
        Assert.True(Math.Abs(actual.X - expected.X) < 0.00001f, $"expected {expected} but got {actual}");
        Assert.True(Math.Abs(actual.Y - expected.Y) < 0.00001f, $"expected {expected} but got {actual}");
        Assert.True(Math.Abs(actual.Z - expected.Z) < 0.00001f, $"expected {expected} but got {actual}");
    }

    [Fact]
    public void PackChildrenInAllParents()
    {
        var src = Load("Models/Root_ABC_allfix.glb");

        var root = CrcTool.FLModelCrc("Root");
        var a = CrcTool.FLModelCrc("A");
        var b = CrcTool.FLModelCrc("B");
        var c = CrcTool.FLModelCrc("C");

        var res = SurfaceBuilder.CreateSur(src);
        Assert.True(res.IsSuccess);

        var sur = res.Data!;

        var meshesC = HullIds(sur, c).ToArray();
        var meshesB = HullIds(sur, b).ToArray();
        var meshesA = HullIds(sur, a).ToArray();
        var meshesRoot = HullIds(sur, root).ToArray();

        AssertUnordered(meshesC, c);
        AssertUnordered(meshesB, b, c);
        AssertUnordered(meshesA, a, b, c);
        AssertUnordered(meshesRoot, root, a, b, c);
    }

    [Fact]
    public void PackChildrenToDynamic()
    {
        var src = Load("Models/b_is_rev.glb");

        var root = CrcTool.FLModelCrc("Root");
        var a = CrcTool.FLModelCrc("A");
        var b = CrcTool.FLModelCrc("B");
        var c = CrcTool.FLModelCrc("C");

        var res = SurfaceBuilder.CreateSur(src);
        Assert.True(res.IsSuccess);

        var sur = res.Data!;

        var meshesC = HullIds(sur, c).ToArray();
        var meshesB = HullIds(sur, b).ToArray();
        var meshesA = HullIds(sur, a).ToArray();
        var meshesRoot = HullIds(sur, root).ToArray();

        AssertUnordered(meshesC, c);
        AssertUnordered(meshesB, b, c);
        AssertUnordered(meshesA, a); //A should not contain B as B is dynamic
        AssertUnordered(meshesRoot, root, a);
    }

    static Vector3 GetHullCenter(uint hull, SurfacePart part)
    {
        var meshB = part.GetHulls(false).First(x => x.HullId == hull);
        Vector3 accum = Vector3.Zero;
        int i = 0;
        foreach (var f in meshB.Faces)
        {
            accum += part.Points[f.Points.A].Point;
            accum += part.Points[f.Points.B].Point;
            accum += part.Points[f.Points.C].Point;
            i += 3;
        }
        return accum / i;
    }

    [Fact]
    public void PackedChildrenTransformed()
    {
        var src = Load("Models/Root_ABC_allfix.glb");

        var root = CrcTool.FLModelCrc("Root");
        var b = CrcTool.FLModelCrc("B");

        var res = SurfaceBuilder.CreateSur(src);
        Assert.True(res.IsSuccess);

        var sur = res.Data!;

        var partRoot = sur.Surfaces.First(x => x.Crc == root);
        var partB = sur.Surfaces.First(x => x.Crc == b);

        // B hull in root should be translated A->B
        AssertEpsilon(new Vector3(5, 0, -5), GetHullCenter(b, partRoot));
        // B hull in self should not be translated
        AssertEpsilon(Vector3.Zero, GetHullCenter(b, partB));
    }

    [Fact]
    public void PackChildHardpoints()
    {
        var src = Load("Models/child_hardpoint.glb");

        var root = CrcTool.FLModelCrc("Root");
        var a = CrcTool.FLModelCrc("A");
        var hp01 = CrcTool.FLModelCrc("Hp01");

        var res = SurfaceBuilder.CreateSur(src);
        Assert.True(res.IsSuccess);

        var sur = res.Data!;

        var partRoot = sur.Surfaces.First(x => x.Crc == root);
        var partA = sur.Surfaces.First(x => x.Crc == a);

        Assert.Contains(hp01, partRoot.HardpointIds);
        Assert.Contains(hp01, partA.HardpointIds);

        Assert.Contains(partRoot.GetHulls(false), x => x.HullId == hp01);
        Assert.Contains(partA.GetHulls(false), x => x.HullId == hp01);
    }
}
