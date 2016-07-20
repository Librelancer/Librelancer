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
		Shader bandShader;
		Vector3 cameraPos;
		float lightingRadius;
		public AsteroidFieldRenderer(AsteroidField field)
		{
			this.field = field;

			//Set up band
			bandShader = ShaderCache.Get("AsteroidBand.vs", "AsteroidBand.frag");
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

		public void Update(ICamera camera)
		{
			vp = camera.ViewProjection;
			cameraPos = camera.Position;
		}

		public void Draw(ResourceManager res, Lighting lighting, CommandBuffer buffer, NebulaRenderer nr)
		{
			//Billboards

			//Band is last
			if (renderBand)
			{
				var tex = (Texture2D)res.FindTexture(field.Band.Shape);
				for (int i = 0; i < SIDES; i++)
				{
					var p = bandCylinder.GetSidePosition(i);
					var zcoord = RenderHelpers.GetZ(bandTransform, cameraPos, p);
					var lt = RenderHelpers.ApplyLights(lighting, p, 1000, nr);
					if (!lt.FogEnabled || VectorMath.Distance(cameraPos, p) <= 1000 + lighting.FogRange.Y)
					{
						buffer.AddCommand(
							bandShader,
							bandShaderDelegate,
							bandShaderCleanup,
							bandTransform,
							new RenderUserData()
							{
								Object = lt,
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
			shader.SetMatrix("World", ref command.World);
			shader.SetMatrix("ViewProjection", ref command.UserData.ViewProjection);
			shader.SetMatrix("NormalMatrix", ref command.UserData.Matrix2);
			shader.SetInteger("Texture", 0);
			shader.SetVector3("CameraPosition", command.UserData.Vector);
			shader.SetColor4("ColorShift", command.UserData.Color);
			shader.SetFloat("TextureAspect", command.UserData.Float);
			RenderMaterial.SetLights(shader, (Lighting)command.UserData.Object);
			command.UserData.Texture.BindTo(0);
			shader.UseProgram();
			state.BlendMode = BlendMode.Normal;
			state.Cull = false;
		}

		static Action<RenderState> bandShaderCleanup = obj => { obj.Cull = true; };
	}
}

