// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Primitives;
using LibreLancer.Render.Materials;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

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

        public AsteroidFieldRenderer(AsteroidField field, SystemRenderer sys)
        {
            this.field = field;
            this.sys = sys;
            //Set up renderDistSq
            float rdist = 0f;
            if (field.Zone.Shape is ZoneSphere)
                rdist = ((ZoneSphere)field.Zone.Shape).Radius;
            else if (field.Zone.Shape is ZoneEllipsoid) {
                var s = ((ZoneEllipsoid)field.Zone.Shape).Size;
                rdist = Math.Max (Math.Max (s.X, s.Y), s.Z);
            }
            else if (field.Zone.Shape is ZoneBox) {
                var s = ((ZoneEllipsoid)field.Zone.Shape).Size;
                rdist = Math.Max (Math.Max (s.X, s.Y), s.Z);
            }

            if (field.BillboardCount != -1)
            {
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
                CreateBufferObject();
            }
            //Set up band
            if (field.Band == null || 
                field.Zone.Shape is not (ZoneSphere or ZoneEllipsoid))
                return;
            bandMaterial = new AsteroidBandMaterial(sys.ResourceManager);
            bandMaterial.Texture = field.Band.Shape;
            bandMaterial.ColorShift = field.Band.ColorShift;
            bandMaterial.TextureAspect = field.Band.TextureAspect;
            renderBand = true;
            bandCylinder = sys.ResourceManager.GetOpenCylinder(SIDES);
        }

        List<VertexPositionNormalDiffuseTexture> verts;
        List<ushort> indices;
        List<DrawCall> cubeDrawCalls = new List<DrawCall>();

        VertexBuffer cube_vbo;
        ElementBuffer cube_ibo;
        class DrawCall
        {
            public Material Material;
            public int StartIndex;
            public int Count;
        }

        float cubeRadius = 0;
        //Code for baking an asteroid cube into a mesh
        void CreateBufferObject()
        {
            verts = new List<VertexPositionNormalDiffuseTexture>();
            indices = new List<ushort>();
            //Gather a list of all materials
            List<Material> mats = new List<Material>();
            if (field.AllowMultipleMaterials)
            {
                foreach (var ast in field.Cube)
                {
                    var f = (ModelFile) ast.Drawable.LoadFile(sys.ResourceManager);
                    f.Initialize(sys.ResourceManager);
                    var l0 = f.Levels[0];
                    for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
                    {
                        var m = l0.Mesh.Meshes[i].Material;
                        bool add = true;
                        foreach (var mat in mats)
                        {
                            if (m.Name == mat.Name)
                            {
                                add = false;
                                break;
                            }
                        }
                        if (add)
                            mats.Add(m);
                    }
                }
            }
            else
            {
                var f = (ModelFile) field.Cube[0].Drawable.LoadFile(sys.ResourceManager);
                f.Initialize(sys.ResourceManager);
                var msh = f.Levels[0];
                mats.Add(msh.Mesh.Meshes[msh.StartMesh].Material);
            }
            //Create the draw calls
            foreach (var mat in mats)
            {
                var start = indices.Count;
                foreach (var ast in field.Cube)
                {
                    AddAsteroidToBuffer(ast, mat, mats.Count == 1);
                }
                var count = indices.Count - start;
                cubeDrawCalls.Add(new DrawCall() { Material = mat, StartIndex = start, Count = count });
            }
            cube_vbo = new VertexBuffer(typeof(VertexPositionNormalDiffuseTexture), verts.Count);
            cube_ibo = new ElementBuffer(indices.Count);
            cube_ibo.SetData(indices.ToArray());
            cube_vbo.SetData(verts.ToArray());
            cube_vbo.SetElementBuffer(cube_ibo);
            verts = null;
            indices = null;
        }

        void AddAsteroidToBuffer(StaticAsteroid ast, Material mat, bool singleMat)
        {
            var model = (ModelFile) ast.Drawable.LoadFile(sys.ResourceManager);
            model.Initialize(sys.ResourceManager);
            var l0 = model.Levels[0];
            var vertType = l0.Mesh.VertexBuffer.VertexType.GetType();
            var transform = ast.RotationMatrix * Matrix4x4.CreateTranslation(ast.Position * field.CubeSize);
            var norm = transform;
            Matrix4x4.Invert(norm, out norm);
            norm = Matrix4x4.Transpose(norm);
            int vertOffset = verts.Count;
            for (int i = 0; i < l0.Mesh.VertexCount; i++) {
                VertexPositionNormalDiffuseTexture vert;
                if (vertType == typeof(VertexPositionNormalDiffuseTexture))
                {
                    vert = l0.Mesh.verticesVertexPositionNormalDiffuseTexture[i];
                }
                else if (vertType == typeof(VertexPositionNormalTexture))
                {
                    var v = l0.Mesh.verticesVertexPositionNormalTexture[i];
                    vert = new VertexPositionNormalDiffuseTexture(
                        v.Position,
                        v.Normal,
                        (uint)Color4.White.ToAbgr(),
                        v.TextureCoordinate);
                }
                else if (vertType == typeof(VertexPositionNormalTextureTwo))
                {
                    var v = l0.Mesh.verticesVertexPositionNormalTextureTwo[i];
                    vert = new VertexPositionNormalDiffuseTexture(
                        v.Position,
                        v.Normal,
                        (uint)Color4.White.ToAbgr(),
                        v.TextureCoordinate);
                }
                else if (vertType == typeof(VertexPositionNormalDiffuseTextureTwo))
                {
                    var v = l0.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[i];
                    vert = new VertexPositionNormalDiffuseTexture(
                        v.Position,
                        v.Normal,
                        v.Diffuse,
                        v.TextureCoordinate);
                }
                else
                {
                   FLLog.Error("Render", "Asteroids: " + vertType.FullName + " not support");
                   return;
                }
                vert.Position = Vector3.Transform(vert.Position, transform);
                cubeRadius = Math.Max(cubeRadius, vert.Position.Length());
                vert.Normal = Vector3.TransformNormal(vert.Normal, norm);
                verts.Add(vert);
            }
            for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
            {
                var m = l0.Mesh.Meshes[i];
                if (m.Material != mat && !singleMat) continue;
                var baseVertex = vertOffset + l0.StartVertex + m.StartVertex;
                int indexStart = m.TriangleStart;
                int indexCount = m.NumRefVertices;
                for (int j = indexStart; j < indexStart + indexCount; j++) {
                    var idx = baseVertex + l0.Mesh.Indices[j];
                    if (idx > ushort.MaxValue) throw new Exception();
                    indices.Add((ushort)idx);
                }
            }
        }

        public void Dispose()
        {
            if (field.Cube.Count > 0)
            {
                cube_ibo.Dispose();
                cube_vbo.Dispose();
            }
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
                    asteroidsTask = Task.Run(() => CalculateAsteroidsTask(camera.Position, camera.Frustum));
                if (field.BillboardCount != -1)
                    billboardTask = Task.Run(() => CalculateBillboards(camera.Position, camera.Frustum));
            }
        }

        ExclusionZone GetExclusionZone(Vector3 pt)
        {
            for (int i = 0; i < field.ExclusionZones.Count; i++) {
                var f = field.ExclusionZones [i];
                if (f.Zone.Shape.ContainsPoint (pt))
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
        void CalculateBillboards(Vector3 position, BoundingFrustum frustum)
        {
            billboardCount = 0;
            
            var close = AsteroidFieldShared.GetCloseCube(cameraPos, (int)(field.FillDist * 2));
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
                        if (field.Zone.Shape.ScaledDistance(center) > 1.1f) continue;
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
                            billboardBuffer[checkCount++].Distance = Vector3.DistanceSquared(center, cameraPos);
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
                if (!frustum.Intersects(sphere)) continue;
                calculatedBillboards[billboardCount++] = billboard;
            }
        }
        
        struct CalculatedCube
        {
            public Vector3 pos;
            public Matrix4x4 tr;
            public CalculatedCube(Vector3 p, Matrix4x4 r) { pos = p; tr = r; }
        }
        private Task asteroidsTask;
        int cubeCount = -1;
        CalculatedCube[] cubes;

        void CalculateAsteroidsTask(Vector3 position, BoundingFrustum frustum)
        {
            cubeCount = 0;
            var close = AsteroidFieldShared.GetCloseCube (cameraPos, field.CubeSize);
            int amountCubes = (int)Math.Floor((field.FillDist / field.CubeSize)) + 1;
            for (int x = -amountCubes; x <= amountCubes; x++) {
                for (int y = -amountCubes; y <= amountCubes; y++) {
                    for (int z = -amountCubes; z <= amountCubes; z++)
                    {
                        var center = close + new Vector3(x,y,z) * field.CubeSize;
                        var closestDistance = (Vector3.Distance(center, position) - cubeRadius);
                        if (closestDistance >= field.FillDist || closestDistance >= lastFog) continue;
                        if (!field.Zone.Shape.ContainsPoint(center)) {
                            continue;
                        }
                       
                        var cubeSphere = new BoundingSphere(center,  cubeRadius);
                        if (!frustum.Intersects(cubeSphere)) {
                            continue;
                        }
                        int tval;
                        if (!AsteroidFieldShared.CubeExists(center, field.EmptyCubeFrequency, out tval)){
                            continue;
                        }
                        if (GetExclusionZone(center) != null) {
                            continue;
                        }
                        cubes[cubeCount++] = new CalculatedCube(center, field.CubeRotation.GetRotation(tval) * Matrix4x4.CreateTranslation(center));
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
                        for (int i = 0; i < cubeDrawCalls.Count; i++)
                        {
                            var dc = cubeDrawCalls[i];
                            if ((Vector3.Distance(center, cameraPos) + cubeRadius) < fadeNear)
                            {
                                buffer.AddCommand(
                                    dc.Material.Render,
                                    null,
                                    buffer.WorldBuffer.SubmitMatrix(ref cubes[j].tr),
                                    lt,
                                    cube_vbo,
                                    PrimitiveTypes.TriangleList,
                                    0,
                                    dc.StartIndex,
                                    dc.Count / 3,
                                    SortLayers.OBJECT
                                );
                                regCount++;
                            }
                            else
                            {
                                buffer.AddCommandFade(
                                    dc.Material.Render,
                                    buffer.WorldBuffer.SubmitMatrix(ref cubes[j].tr),
                                    lt,
                                    cube_vbo,
                                    PrimitiveTypes.TriangleList,
                                    dc.StartIndex,
                                    dc.Count / 3,
                                    SortLayers.OBJECT,
                                    new Vector2(fadeNear, fadeFar),
                                    z
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
                        billboardTex = (Texture2D)res.FindTexture (field.BillboardShape.Texture);
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
                if (!_camera.Frustum.Intersects(new BoundingSphere(field.Zone.Position, lightingRadius)))
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
            if (field.Zone.Shape is ZoneSphere s)
                sz = new Vector3(s.Radius);
            else if (field.Zone.Shape is ZoneEllipsoid e)
                sz = e.Size;
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