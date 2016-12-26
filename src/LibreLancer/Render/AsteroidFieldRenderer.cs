/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.GameData;
using LibreLancer.Primitives;
namespace LibreLancer
{
	public class AsteroidFieldRenderer
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
			bandCylinder = new OpenCylinder(SIDES);
			bandNormal = bandTransform;
			bandNormal.Invert();
			bandNormal.Transpose();
		}


		ICamera _camera;
		public void Update(ICamera camera)
		{
			vp = camera.ViewProjection;
			cameraPos = camera.Position;
			_camera = camera;
			for (int i = 0; i < field.Cube.Count; i++)
				field.Cube [i].Drawable.Update (camera, TimeSpan.Zero);
			if (VectorMath.DistanceSquared (cameraPos, field.Zone.Position) <= renderDistSq) {
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
				var dist = r.rand.NextFloat (r.field.BillboardDistance, r.field.FillDist);
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
			public Vector3 rot;
			public CalculatedCube(Vector3 p, Vector3 r) { pos = p; rot = r; }
		}
		Action _asteroidsCalculation;
		bool _asteroidsCalculated = false;
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
			var close = AsteroidFieldShared.GetCloseCube (cameraPos, field.CubeSize);
			var cubeRad = new Vector3 (field.CubeSize) * 0.5f;
			int amountCubes = (int)Math.Floor((field.FillDist / field.CubeSize)) + 1;
			for (int x = -amountCubes; x <= amountCubes; x++) {
				for (int y = -amountCubes; y <= amountCubes; y++) {
					for (int z = -amountCubes; z <= amountCubes; z++) {
						var center = close + new Vector3 (x * field.CubeSize, y * field.CubeSize, z * field.CubeSize);
						if (!field.Zone.Shape.ContainsPoint (center))
							continue;
						if (GetExclusionZone (center) != null)
							continue;
						float tval;
						if (!AsteroidFieldShared.CubeExists (center, field.EmptyCubeFrequency, out tval))
							continue;
						var cubeBox = new BoundingBox(center - cubeRad, center + cubeRad);
						if (!frustum.Intersects (cubeBox))
							continue;
						cubes[cubeCount++] = new CalculatedCube(center, field.CubeRotation.GetRotation(tval));
					}
				}
			}
			_asteroidsCalculated = true;
		}

		Texture2D billboardTex;
		static readonly Vector2[][] billboardCoords =  {
			new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(1,0) },
			new []{ new Vector2(0.5f,0.5f), new Vector2(0,0),  new Vector2(0,1) },
			new []{ new Vector2(0.5f,0.5f), new Vector2(0,1),  new Vector2(1,1) },
			new []{ new Vector2(0.5f,0.5f), new Vector2(1,0),  new Vector2(1,1) }
		};
		public void Draw(ResourceManager res, Lighting lighting, CommandBuffer buffer, NebulaRenderer nr)
		{
            //Null check
            if (_camera == null)
                return;
			//Asteroids!
			if (VectorMath.DistanceSquared (cameraPos, field.Zone.Position) <= renderDistSq) {
				if (cubeCount == -1)
					return;
				while (!_asteroidsCalculated) {
				}
				for (int j = 0; j < cubeCount; j++) {
					for (int i = 0; i < field.Cube.Count; i++) {
						var c = field.Cube [i];
						var center = cubes[j].pos;
						var rotmat = Matrix4.CreateRotationX(cubes[j].rot.X) *
											Matrix4.CreateRotationY(cubes[j].rot.Y) *
											Matrix4.CreateRotationZ(cubes[j].rot.Z);
						var astpos = center + rotmat.Transform((c.Position * field.CubeSize));
						var r = c.Drawable.GetRadius ();
						if (_camera.Frustum.Intersects (new BoundingSphere (astpos, r))) {
							var lt = RenderHelpers.ApplyLights (lighting, astpos, r, nr);
							if (!lt.FogEnabled || VectorMath.DistanceSquared (cameraPos, astpos) <= (r + lt.FogRange.Y) * (r + lt.FogRange.Y))
								c.Drawable.DrawBuffer (buffer, rotmat * c.RotationMatrix * Matrix4.CreateTranslation (astpos), lt);
						}
					}
				}
				if (field.BillboardCount != -1) {	
					if (billboardTex == null || billboardTex.IsDisposed)
						billboardTex = (Texture2D)res.FindTexture (field.BillboardShape.Texture);
				
					for (int i = 0; i < astbillboards.Length; i++) {
						if (!astbillboards [i].Inited) {
							astbillboards [i].Spawn (this);
						}
						var d = VectorMath.DistanceSquared (cameraPos, astbillboards [i].Position);
						if (d < (field.BillboardDistance * field.BillboardDistance) || d > (field.FillDist * field.FillDist))
							astbillboards [i].Spawn (this);
						if (astbillboards [i].Visible) {
							var alpha = 1f;
							var coords = billboardCoords [astbillboards [i].Texture];
							sys.Game.Billboards.DrawTri (
								billboardTex,
								astbillboards [i].Position,
								astbillboards[i].Size,
								new Color4(field.BillboardTint, alpha),
								coords[0], coords[2], coords[1],
								0,
								SortLayers.OBJECT
							);
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
					var lt = RenderHelpers.ApplyLights(lighting, p, lightingRadius, nr);
					if (!lt.FogEnabled || VectorMath.DistanceSquared(cameraPos, p) <= (lightingRadius + lt.FogRange.Y) * (lightingRadius + lt.FogRange.Y))
					{
						buffer.AddCommand(
							bandShader.Shader,
							bandShaderDelegate,
							bandShaderCleanup,
							bandTransform,
							new RenderUserData()
							{
								Lighting = lt,
								Float = field.Band.TextureAspect,
								Color = field.Band.ColorShift,
								ViewProjection = vp,
								Texture = tex,
								Vector = cameraPos,
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
		static Action<Shader, RenderState, RenderCommand> bandShaderDelegate = BandShaderSetup;
		static void BandShaderSetup(Shader shader, RenderState state, RenderCommand command)
		{
			bandShader.SetWorld(ref command.World);
			bandShader.SetViewProjection(ref command.UserData.ViewProjection);
			bandShader.SetNormalMatrix(ref command.UserData.Matrix2);
			shader.SetInteger(_bsTexture, 0);
			shader.SetVector3(_bsCameraPosition, command.UserData.Vector);
			shader.SetColor4(_bsColorShift, command.UserData.Color);
			shader.SetFloat(_bsTextureAspect, command.UserData.Float);
			RenderMaterial.SetLights(bandShader, command.UserData.Lighting);
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

