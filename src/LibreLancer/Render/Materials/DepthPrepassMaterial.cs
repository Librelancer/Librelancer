using System;
using LibreLancer.Vertices;

namespace LibreLancer
{
    public class DepthPrepassMaterial : RenderMaterial
    {
        public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
        {
            Shaders.ShaderVariables sh = Shaders.DepthPass_Normal.Get();
            //These things don't have normals
            rstate.BlendMode = BlendMode.Opaque;
            sh.SetViewProjection(Camera);
            //Dt
            sh.SetWorld(World);
            sh.UseProgram();
        }

        public override void ApplyDepthPrepass(RenderState rstate)
        {
            throw new NotImplementedException();
        }

        public override bool IsTransparent
        {
            get
            {
                return false;
            }
        }
    }
}