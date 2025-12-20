// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Primitives;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render
{
    public class AsteroidFieldRenderer : IDisposable
    {
        const int SIDES = 20;

        AsteroidField field;
        bool renderBand = false;
        Matrix4x4 bandTransform;
        OpenCylinder bandCylinder;
        Vector3 cameraPos;
        float lightingRadius;
        float renderDistSq;
        Random rand = new Random();
        SystemRenderer sys;
        private AsteroidBandMaterial bandMaterial;
        private AsteroidCubeMesh cubeMesh;

        TextureShape billboardShape;

        public AsteroidFieldRenderer(AsteroidField field, SystemRenderer sys)
        {
            this.field = field;
            this.sys = sys;
            //Set up renderDistSq
            float rdist = Math.Max (Math.Max (field.Zone.Size.X, field.Zone.Size.Y), field.Zone.Size.Z);

            if (field.BillboardCount != -1)
            {
                SetBillboardShape();
                billboardCube = new AsteroidBillboard[field.BillboardCount];
                for(int i = 0; i < field.BillboardCount; i++)
                    billboardCube[i].Spawn(this);
                calculatedBillboards = new AsteroidBillboard[field.BillboardCount];
            }

            rdist += field.FillDist;
            renderDistSq = rdist * rdist;
            cubes = new CalculatedCube[4000];
            if (field.Cube.Count > 0)
            {
                cubeMesh = new AsteroidCubeMeshBuilder().CreateMesh(sys.RenderContext, field, sys.ResourceManager);
            }
            //Set up band
            if (field.Band == null ||
                (field.Zone.Shape != ShapeKind.Sphere && field.Zone.Shape != ShapeKind.Ellipsoid))
                return;
            bandMaterial = new AsteroidBandMaterial(sys.ResourceManager);
            bandMaterial.Texture = field.Band.Shape;
            bandMaterial.ColorShift = field.Band.ColorShift;
            bandMaterial.TextureAspect = field.Band.TextureAspect;
            renderBand = true;
            bandCylinder = sys.ResourceManager.GetOpenCylinder(SIDES);
        }


        void SetBillboardShape()
        {
            var col = new TexturePanelCollection();
            foreach (var f in field.TexturePanels)
            {
                f.Load(sys.ResourceManager);
                col.AddFile(f);
            }

            billboardShape = col.GetShape(field.BillboardShape);
        }


        public void Dispose()
        {
            cubeMesh?.Dispose();
        }

        private float lastFog = float.MaxValue;
        private ICamera _camera;
        public void Update(ICamera camera)
        {
            _camera = camera;
            cameraPos = camera.Position;
            if (Vector3.DistanceSquared(cameraPos, field.Zone.Position) <= renderDistSq)
            {
                if (field.Cube.Count > 0)
                    asteroidsTask = Task.Run(() => CalculateAsteroidsTask(camera));
                if (field.BillboardCount != -1)
                    billboardTask = Task.Run(() => CalculateBillboards(camera));
            }
        }

        AsteroidExclusionZone GetExclusionZone(Vector3 pt)
        {
            for (int i = 0; i < field.ExclusionZones.Count; i++) {
                var f = field.ExclusionZones [i];
                if (f.Zone.ContainsPoint (pt))
                    return f;
            }
            return null;
        }
        struct AsteroidBillboard : IComparable<AsteroidBillboard>
        {
            public Vector3 Position;
            public float Size;
            public int Texture;
            public float Distance;
            public void Spawn(AsteroidFieldRenderer r)
            {
                var min = 0;
                var p = new Vector3(
                    r.rand.NextFloat(-1,1),
                    r.rand.NextFloat(-1,1),
                    r.rand.NextFloat(-1,1)
                );
                Position = (p * r.field.FillDist);
                Size = r.rand.NextFloat (r.field.BillboardSize.X, r.field.BillboardSize.Y) * 2;
                Texture = r.rand.Next (0, 3);
            }

            public int CompareTo(AsteroidBillboard other) => Distance.CompareTo(other.Distance);
        }
        /*
         * Asteroid billboards are generated in a cube of size fillDist * 2
         * This is up to billboard_count billboards
         * The billboards spawn from 110% of the distance to the center
         */
        private AsteroidBillboard[] billboardCube;
        private AsteroidBillboard[] calculatedBillboards;
        private AsteroidBillboard[] billboardBuffer = new AsteroidBillboard[9000];
        private int billboardCount = 0;
        private Task billboardTask;
        private bool warnedTooManyBillboards = false;
        void CalculateBillboards(ICamera camera)
        {
            billboardCount = 0;
            var position = camera.Position;
            var close = AsteroidFieldShared.GetCloseCube(position, (int)(field.FillDist * 2));
            var checkRad = field.FillDist + field.BillboardSize.Y;
            int checkCount = 0;
            for (var x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var center = close + new Vector3(x, y, z) * (field.FillDist * 2);
                        //early bail for billboards too far
                        if (Vector3.Distance(position, center) - checkRad > field.FillDist) continue;
                        //bail billboards outside of zone - avoids popping
                        if (field.Zone.ScaledDistance(center) > 1.1f) continue;
                        //rotate
                        var rotation =
                            AsteroidCubeRotation.Default.GetRotation((int)(AsteroidFieldShared.PositionHash(center) * 63));
                        for (int i = 0; i < billboardCube.Length; i++)
                        {
                            var spritepos = center + Vector3.Transform(billboardCube[i].Position, rotation);
                            //cull individual billboards too far
                            if(Vector3.Distance(position, spritepos) > field.FillDist) continue;
                            billboardBuffer[checkCount] = billboardCube[i];
                            billboardBuffer[checkCount].Position = spritepos;
                            billboardBuffer[checkCount++].Distance = Vector3.DistanceSquared(center, position);
                        }
                    }
                }
            }
            //Highly unlikely this check will succeed. If it does there's something wrong with the cube code
            if (checkCount > field.BillboardCount) {
                if (!warnedTooManyBillboards)
                {
                    warnedTooManyBillboards = true;
                    FLLog.Warning("Asteroids", "Too many billboards in sort task for field " + field.Zone.Nickname);
                }
                Array.Sort(billboardBuffer, 0, checkCount); //Get closest
                checkCount = field.BillboardCount;
            }
            //Cull ones that aren't on screen
            for (int i = 0; i < checkCount; i++)
            {
                var billboard = billboardBuffer[i];
                var sphere = new BoundingSphere(billboard.Position, billboard.Size * 1.5f);
                if (!camera.FrustumCheck(sphere)) continue;
                calculatedBillboards[billboardCount++] = billboard;
            }
        }

        struct CalculatedCube
        {
            public Vector3 pos;
            public Matrix4x4 tr;
            public CalculatedCube(Vector3 p, Transform3D r) { pos = p; tr = r.Matrix(); }
        }
        private Task asteroidsTask;
        int cubeCount = -1;
        CalculatedCube[] cubes;

        void CalculateAsteroidsTask(ICamera cam)
        {
            cubeCount = 0;
            var position = cam.Position;
            var close = AsteroidFieldShared.GetCloseCube (cameraPos, field.CubeSize);
            int amountCubes = (int)Math.Floor((field.FillDist / field.CubeSize)) + 1;
            for (int x = -amountCubes; x <= amountCubes; x++) {
                for (int y = -amountCubes; y <= amountCubes; y++) {
                    for (int z = -amountCubes; z <= amountCubes; z++)
                    {
                        var center = close + new Vector3(x,y,z) * field.CubeSize;
                        var closestDistance = (Vector3.Distance(center, position) - cubeMesh.Radius);
                        if (closestDistance >= field.FillDist || closestDistance >= lastFog) continue;
                        if (!field.Zone.ContainsPoint(center)) {
                            continue;
                        }

                        var cubeSphere = new BoundingSphere(center,  cubeMesh.Radius);
                        if (!cam.FrustumCheck(cubeSphere)) {
                            continue;
                        }
                        int tval;
                        if (!AsteroidFieldShared.CubeExists(center, field.EmptyCubeFrequency, out tval)){
                            continue;
                        }
                        if (GetExclusionZone(center) != null) {
                            continue;
                        }

                        cubes[cubeCount++] =
                            new CalculatedCube(center, new Transform3D(center, field.CubeRotation.GetRotation(tval)));
                    }
                }
            }
        }
        Texture2D billboardTex;
        static readonly Vector2[][] billboardCoords =  {
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(1,0) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(0,1) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,1),  new Vector2(1,1) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(1,0),  new Vector2(1,1) }
        };
        public void Draw(ResourceManager res, SystemLighting lighting, CommandBuffer buffer, NebulaRenderer nr)
        {
            //Asteroids!
            if (Vector3.DistanceSquared (cameraPos, field.Zone.Position) <= renderDistSq)
            {
                float fadeNear = field.FillDist - 100f;
                float fadeFar = field.FillDist;
                if (field.Cube.Count > 0)
                {
                    if (cubeCount == -1)
                        return;
                    asteroidsTask.Wait();
                    var lt = RenderHelpers.ApplyLights(lighting, 0, cameraPos, field.FillDist, nr);
                    lt.Ambient += field.AmbientIncrease.Rgb;
                    lt.Ambient *= field.AmbientColor.Rgb;
                    if (lt.FogMode == FogModes.Linear)
                        lastFog = lt.FogRange.Y;
                    else
                        lastFog = float.MaxValue;
                    int fadeCount = 0;
                    int regCount = 0;
                    for (int j = 0; j < cubeCount; j++)
                    {
                        var center = cubes[j].pos;
                        var z = RenderHelpers.GetZ(cameraPos, center);
                        for (int i = 0; i < cubeMesh.Drawcalls.Length; i++)
                        {
                            var dc = cubeMesh.Drawcalls[i];
                            var mat = res.FindMaterial(dc.MaterialCrc);
                            if ((Vector3.Distance(center, cameraPos) + cubeMesh.Radius) < fadeNear)
                            {
                                buffer.AddCommand(
                                    mat.Render,
                                    null,
                                    buffer.WorldBuffer.SubmitMatrix(ref cubes[j].tr),
                                    lt,
                                    cubeMesh.VertexBuffer,
                                    PrimitiveTypes.TriangleList,
                                    dc.BaseVertex,
                                    dc.StartIndex,
                                    dc.Count / 3,
                                    SortLayers.OBJECT,
                                    0, null, 0, BasicMaterial.SetDc(field.DiffuseColor)
                                );
                                regCount++;
                            }
                            else
                            {
                                buffer.AddCommandFade(
                                    mat.Render,
                                    buffer.WorldBuffer.SubmitMatrix(ref cubes[j].tr),
                                    lt,
                                    cubeMesh.VertexBuffer,
                                    PrimitiveTypes.TriangleList,
                                    dc.BaseVertex,
                                    dc.StartIndex,
                                    dc.Count / 3,
                                    SortLayers.OBJECT,
                                    new Vector2(fadeNear, fadeFar),
                                    z, 0, BasicMaterial.SetDc(field.DiffuseColor)
                                );
                                fadeCount++;
                            }
                        }
                    }
                }
                if (field.BillboardCount != -1)
                {
                    var cameraLights = RenderHelpers.ApplyLights(lighting, 0, cameraPos, 1, nr);
                    if (billboardTex == null || billboardTex.IsDisposed)
                        billboardTex = (Texture2D)res.FindTexture (billboardShape.Texture);
                    billboardTask.Wait();
                    for (int i = 0; i < billboardCount; i++)
                    {
                        var alpha = BillboardAlpha(Vector3.Distance(calculatedBillboards[i].Position, cameraPos));
                        if(alpha <= 0) continue;
                        var coords = billboardCoords [calculatedBillboards [i].Texture];
                        sys.Billboards.DrawTri (
                            billboardTex,
                            calculatedBillboards [i].Position,
                            calculatedBillboards[i].Size,
                            new Color4(field.BillboardTint * cameraLights.Ambient, alpha),
                            coords[0], coords[2], coords[1],
                            0,
                            SortLayers.OBJECT
                         );
                    }
                }

            }
            //Band is last
            if (renderBand)
            {
                CalculateBandTransform();
                if (!_camera.FrustumCheck(new BoundingSphere(field.Zone.Position, lightingRadius)))
                    return;
                var bandHandle = buffer.WorldBuffer.SubmitMatrix(ref bandTransform);
                for (int i = 0; i < SIDES; i++)
                {
                    var p = bandCylinder.GetSidePosition(i);
                    var zcoord = RenderHelpers.GetZ(bandTransform, cameraPos, p);
                    p = Vector3.Transform(p, bandTransform);
                    var lt = RenderHelpers.ApplyLights(lighting, 0, p, lightingRadius, nr);
                    if (lt.FogMode != FogModes.Linear || Vector3.DistanceSquared(cameraPos, p) <= (lightingRadius + lt.FogRange.Y) * (lightingRadius + lt.FogRange.Y))
                    {
                        buffer.AddCommand(bandMaterial, null, bandHandle, lt, bandCylinder.VertexBuffer,
                            PrimitiveTypes.TriangleList, 0, i * 6, 2, SortLayers.OBJECT, zcoord);
                    }
                }
            }
        }
        void CalculateBandTransform()
        {
            Vector3 sz = Vector3.Zero;
            if (field.Zone.Shape == ShapeKind.Sphere)
                sz = new Vector3(field.Zone.Size.X);
            else
                sz = field.Zone.Size;
            sz.X -= field.Band.OffsetDistance;
            sz.Z -= field.Band.OffsetDistance;
            lightingRadius = Math.Max(sz.X, sz.Z);
            bandTransform = (
                Matrix4x4.CreateScale(sz.X, field.Band.Height / 2f, sz.Z) *
                field.Zone.RotationMatrix *
                Matrix4x4.CreateTranslation(field.Zone.Position)
            );
        }
        float BillboardAlpha(float dist)
        {
            if (dist >= field.BillboardDistance) {
                //Fade out from billboard_distance to filldist
                return (field.FillDist - dist) / (field.FillDist - field.BillboardDistance);
            }
            //visible from start_dist - start_dist * fade percentage
            var fadeNear = field.BillboardDistance - (field.BillboardDistance * field.BillboardFadePercentage);
            if (dist >= fadeNear)
            {
                var max = field.BillboardDistance * field.BillboardFadePercentage;
                return (dist - fadeNear) / max;
            }
            //Too close to the camera: invisible
            return 0;
        }
    }
}
