// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.GameData;
using LibreLancer.Primitives;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer
{
    public class AsteroidFieldRenderer : IDisposable
    {
        const int SIDES = 20;

        AsteroidField field;
        bool renderBand = false;
        Matrix4 bandTransform;
        OpenCylinder bandCylinder;
        Matrix4 vp;
        Matrix4 bandNormal;
        static ShaderVariables bandShader;
        static int _bsTexture;
        static int _bsCameraPosition;
        static int _bsColorShift;
        static int _bsTextureAspect;
        Vector3 cameraPos;
        float lightingRadius;
        float renderDistSq;
        AsteroidBillboard[] astbillboards;
        Random rand = new Random();
        SystemRenderer sys;

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
                astbillboards = new AsteroidBillboard[field.BillboardCount];
            rdist += field.FillDist;
            renderDistSq = rdist * rdist;
            cubes = new CalculatedCube[1000];
            _asteroidsCalculation = CalculateAsteroids;
            if (field.Cube.Count > 0)
            {
                CreateBufferObject();
            }
            //Set up band
            if (field.Band == null)
                return;
            if (bandShader == null)
            {
                bandShader = ShaderCache.Get("AsteroidBand.vs", "AsteroidBand.frag");
                _bsTexture = bandShader.Shader.GetLocation("Texture");
                _bsCameraPosition = bandShader.Shader.GetLocation("CameraPosition");
                _bsColorShift = bandShader.Shader.GetLocation("ColorShift");
                _bsTextureAspect = bandShader.Shader.GetLocation("TextureAspect");
            }
            Vector3 sz;
            if (field.Zone.Shape is ZoneSphere)
                sz = new Vector3(((ZoneSphere)field.Zone.Shape).Radius);
            else if (field.Zone.Shape is ZoneEllipsoid)
                sz = ((ZoneEllipsoid)field.Zone.Shape).Size;
            else
                return;
            sz.Xz -= new Vector2(field.Band.OffsetDistance);
            lightingRadius = Math.Max(sz.X, sz.Z);
            renderBand = true;
            bandTransform = (
                Matrix4.CreateScale(sz.X, field.Band.Height / 2, sz.Z) * 
                field.Zone.RotationMatrix * 
                Matrix4.CreateTranslation(field.Zone.Position)
            );
            bandCylinder = sys.ResourceManager.GetOpenCylinder(SIDES);
            bandNormal = bandTransform;
            bandNormal.Invert();
            bandNormal.Transpose();
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
                    var l0 = (ast.Drawable as ModelFile).Levels[0];
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
                var msh = (field.Cube[0].Drawable as ModelFile).Levels[0];
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
            var model = ast.Drawable as ModelFile;
            var l0 = model.Levels[0];
            var vertType = l0.Mesh.VertexBuffer.VertexType.GetType();
            var transform = ast.RotationMatrix * Matrix4.CreateTranslation(ast.Position * field.CubeSize);
            var norm = transform;
            norm.Invert();
            norm.Transpose();
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
                        (uint)Color4.White.ToArgb(),
                        v.TextureCoordinate);
                }
                else if (vertType == typeof(VertexPositionNormalTextureTwo))
                {
                    var v = l0.Mesh.verticesVertexPositionNormalTextureTwo[i];
                    vert = new VertexPositionNormalDiffuseTexture(
                        v.Position,
                        v.Normal,
                        (uint)Color4.White.ToArgb(),
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
                    throw new NotImplementedException("Asteroids: " + vertType.FullName);
                }
                vert.Position = VectorMath.Transform(vert.Position, transform);
                vert.Normal = (norm * new Vector4(vert.Normal, 0)).Xyz;
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

        ICamera _camera;
        public void Update(ICamera camera)
        {
            vp = camera.ViewProjection;
            cameraPos = camera.Position;
            _camera = camera;
            //for (int i = 0; i < field.Cube.Count; i++)
                //field.Cube [i].Drawable.Update (camera, TimeSpan.Zero);
            if (field.Cube.Count > 0 && VectorMath.DistanceSquared (cameraPos, field.Zone.Position) <= renderDistSq) {
                _asteroidsCalculated = false;
                cubeCount = 0;
                AsyncManager.RunTask (_asteroidsCalculation);
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
        struct AsteroidBillboard
        {
            public Vector3 Position;
            public bool Visible;
            public bool Inited;
            public float Size;
            public int Texture;
            public void Spawn(AsteroidFieldRenderer r)
            {
                Inited = true;
                var min = r.field.FillDist * (1 - (r.field.BillboardFadePercentage * 0.1f));
                var dist = r.rand.NextFloat (min, r.field.FillDist);
                var theta = r.rand.NextFloat(0, (float)Math.PI * 2);
                var phi = r.rand.NextFloat(0, (float)Math.PI * 2);
                var p = new Vector3(
                    (float)(Math.Sin(phi) * Math.Cos(theta)),
                    (float)(Math.Sin(phi) * Math.Sin(theta)),
                    (float)(Math.Cos(phi))
                );
                var directional = (p * dist);
                Position = directional + r.cameraPos;
                Visible = r.field.Zone.Shape.ContainsPoint (Position) 
                    && (r.GetExclusionZone (Position) == null);
                Size = r.rand.NextFloat (r.field.BillboardSize.X, r.field.BillboardSize.Y) * 2;
                Texture = r.rand.Next (0, 3);
            }
        }
        struct CalculatedCube
        {
            public Vector3 pos;
            public Matrix4 tr;
            public CalculatedCube(Vector3 p, Matrix4 r) { pos = p; tr = r; }
        }
        Action _asteroidsCalculation;
        volatile bool _asteroidsCalculated = false;
        int cubeCount = -1;
        CalculatedCube[] cubes;
        void CalculateAsteroids()
        {
            Vector3 position;
            BoundingFrustum frustum;
            lock (_camera) {
                position = _camera.Position;
                frustum = _camera.Frustum;
            }
            ZfrustumCulled = ZexistCulled = ZexcludeCulled = ZshapeCulled = 0;
            var close = AsteroidFieldShared.GetCloseCube (cameraPos, field.CubeSize);
            var cubeRad = new Vector3 (field.CubeSize) * 0.5f;
            int amountCubes = (int)Math.Floor((field.FillDist / field.CubeSize));
            for (int x = -amountCubes; x <= amountCubes; x++) {
                for (int y = -amountCubes; y <= amountCubes; y++) {
                    for (int z = -amountCubes; z <= amountCubes; z++)
                    {
                        var center = close + new Vector3(x * field.CubeSize, y * field.CubeSize, z * field.CubeSize);
                        if (!field.Zone.Shape.ContainsPoint(center)) {
                            continue;
                        }
                        //var cubeBox = new BoundingBox(center - cubeRad, center + cubeRad);
                        //if (!frustum.Intersects(cubeBox))
                        //continue;
                        var cubeSphere = new BoundingSphere(center, field.CubeSize * 0.5f);
                        if (!frustum.Intersects(cubeSphere)) {
                            continue;
                        }
                        float tval;
                        if (!AsteroidFieldShared.CubeExists(center, field.EmptyCubeFrequency, out tval)){
                            continue;
                        }
                        if (GetExclusionZone(center) != null) {
                            continue;
                        }
                        cubes[cubeCount++] = new CalculatedCube(center, field.CubeRotation.GetRotation(tval) * Matrix4.CreateTranslation(center));
                    }
                }
            }
            _asteroidsCalculated = true;
        }
        volatile int ZfrustumCulled = 0;
        volatile int ZexistCulled = 0;
        volatile int ZshapeCulled = 0;
        volatile int ZexcludeCulled = 0;
        Texture2D billboardTex;
        static readonly Vector2[][] billboardCoords =  {
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(1,0) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(0,1) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(0,1),  new Vector2(1,1) },
            new []{ new Vector2(0.5f,0.5f), new Vector2(1,0),  new Vector2(1,1) }
        };
        public void Draw(ResourceManager res, SystemLighting lighting, CommandBuffer buffer, NebulaRenderer nr)
        {
            //Null check
            if (_camera == null)
                return;
            //Asteroids!
            if (VectorMath.DistanceSquared (cameraPos, field.Zone.Position) <= renderDistSq) {
                float fadeNear = field.FillDist * 0.9f;
                float fadeFar = field.FillDist;
                if (field.Cube.Count > 0)
                {
                    if (cubeCount == -1)
                        return;
                    for (int i = 0; i < cubeDrawCalls.Count; i++)
                        cubeDrawCalls[i].Material.Update(_camera);
                    var lt = RenderHelpers.ApplyLights(lighting, 0, cameraPos, field.FillDist, nr);
                    while (!_asteroidsCalculated)
                    {
                    }
                    for (int j = 0; j < cubeCount; j++)
                    {
                        var center = cubes[j].pos;
                        var z = RenderHelpers.GetZ(cameraPos, center);
                        for (int i = 0; i < cubeDrawCalls.Count; i++)
                        {
                            var dc = cubeDrawCalls[i];
                            if (VectorMath.DistanceSquared(center, cameraPos) < (fadeNear * fadeNear))
                            { //TODO: Accurately determine whether or not a cube has fading
                            }
                            buffer.AddCommandFade(
                                dc.Material.Render,
                                cubes[j].tr,
                                lt,
                                cube_vbo,
                                PrimitiveTypes.TriangleList,
                                dc.StartIndex,
                                dc.Count / 3,
                                SortLayers.OBJECT,
                                new Vector2(fadeNear, fadeFar),
                                z
                            );
                        }
                    }
                }
                if (field.BillboardCount != -1 || false) {
                    var cameraLights = RenderHelpers.ApplyLights(lighting, 0, cameraPos, 1, nr);
                    if (billboardTex == null || billboardTex.IsDisposed)
                        billboardTex = (Texture2D)res.FindTexture (field.BillboardShape.Texture);
                    var bdSq = field.BillboardDistance * field.BillboardDistance;
                    var fadePctSq = bdSq * field.BillboardFadePercentage;
                    var fillDistSq = field.FillDist * field.FillDist;
                    for (int i = 0; i < astbillboards.Length; i++) {
                        if (!astbillboards [i].Inited) {
                            astbillboards [i].Spawn (this);
                        }
                        var dSq = VectorMath.DistanceSquared (cameraPos, astbillboards [i].Position);
                        if (dSq > fillDistSq)
                            astbillboards [i].Spawn (this);
                        if (astbillboards [i].Visible) {
                            var alpha = 1f;
                            var fnear = dSq - bdSq;
                            if(fnear < fadePctSq) {
                                alpha = fnear / fadePctSq;
                            }
                            var ffar = fillDistSq - dSq;
                            if(ffar < fadePctSq) {
                                alpha = ffar / fadePctSq;
                            }
                            var coords = billboardCoords [astbillboards [i].Texture];
                            /*sys.Billboards.DrawTri (
                                billboardTex,
                                astbillboards [i].Position,
                                astbillboards[i].Size,
                                new Color4(field.BillboardTint * cameraLights.Ambient, alpha),
                                coords[0], coords[2], coords[1],
                                0,
                                SortLayers.OBJECT
                            );*/
                        }
                    }
                }

            }

            //Band is last
            if (renderBand)
            {
                if (!_camera.Frustum.Intersects(new BoundingSphere(field.Zone.Position, lightingRadius)))
                    return;
                var tex = (Texture2D)res.FindTexture(field.Band.Shape);
                for (int i = 0; i < SIDES; i++)
                {
                    var p = bandCylinder.GetSidePosition(i);
                    var zcoord = RenderHelpers.GetZ(bandTransform, cameraPos, p);
                    p = bandTransform.Transform(p);
                    var lt = RenderHelpers.ApplyLights(lighting, 0, p, lightingRadius, nr);
                    if (lt.FogMode != FogModes.Linear || VectorMath.DistanceSquared(cameraPos, p) <= (lightingRadius + lt.FogRange.Y) * (lightingRadius + lt.FogRange.Y))
                    {
                        buffer.AddCommand(
                            bandShader.Shader,
                            bandShaderDelegate,
                            bandShaderCleanup,
                            bandTransform,
                            lt,
                            new RenderUserData()
                            {
                                Float = field.Band.TextureAspect,
                                Color = field.Band.ColorShift,
                                Camera = _camera,
                                Texture = tex,
                                Matrix2 = bandNormal
                            },
                            bandCylinder.VertexBuffer,
                            PrimitiveTypes.TriangleList,
                            0,
                            i * 6,
                            2,
                            true,
                            SortLayers.OBJECT,
                            zcoord
                        );
                    }
                }
            }
        }
        static ShaderAction bandShaderDelegate = BandShaderSetup;
        static void BandShaderSetup(Shader shader, RenderState state, ref RenderCommand command)
        {
            bandShader.SetWorld(ref command.World);
            var vp = command.UserData.Camera.ViewProjection;
            bandShader.SetViewProjection(ref vp);
            bandShader.SetNormalMatrix(ref command.UserData.Matrix2);
            shader.SetInteger(_bsTexture, 0);
            shader.SetVector3(_bsCameraPosition, command.UserData.Camera.Position);
            shader.SetColor4(_bsColorShift, command.UserData.Color);
            shader.SetFloat(_bsTextureAspect, command.UserData.Float);
            RenderMaterial.SetLights(bandShader, ref command.Lights);
            command.UserData.Texture.BindTo(0);
            shader.UseProgram();
            state.BlendMode = BlendMode.Normal;
            state.Cull = false;
        }

        static Action<RenderState> bandShaderCleanup = BandShaderCleanup;
        static void BandShaderCleanup(RenderState state)
        {
            state.Cull = true;
        }
    }
}

