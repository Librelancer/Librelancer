// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer
{
	public class LoadingDataState : GameState
	{
		Texture2D splash;
		public LoadingDataState(FreelancerGame g) : base(g)
		{
			splash = g.GameData.GetSplashScreen();
		}
        bool shadersCompiled = false;
        int xCnt = 0;
        public override void Draw(TimeSpan delta)
		{
            xCnt++;
			Game.Renderer2D.Start(Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched(splash, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White, true);
			Game.Renderer2D.Finish();
            if (!shadersCompiled && (xCnt >= 5))
            {
                CompileShaders();
                shadersCompiled = true;
            }
        }
		public override void Update(TimeSpan delta)
		{
            if (Game.InitialLoadComplete)
			{
				Game.ResourceManager.Preload();
				Game.Fonts.LoadFonts();
				if (Game.Config.CustomState != null)
					Game.ChangeState(Game.Config.CustomState(Game));
				else
					Game.ChangeState(new LuaMenu(Game));
			}
		}

        void CompileShaders()
        {
            Shaders("Atmosphere.vs", "AtmosphereMaterial_PositionTexture.frag");
            Shaders("Basic_PositionNormalTexture.vs", "Basic_Fragment.frag", ShaderCaps.Spotlight, ShaderCaps.EtEnabled, ShaderCaps.FadeEnabled, ShaderCaps.AlphaTestEnabled, ShaderCaps.VertexLighting);
            Shaders("Basic_PositionNormalTextureTwo.vs", "Basic_Fragment.frag", ShaderCaps.Spotlight, ShaderCaps.EtEnabled, ShaderCaps.FadeEnabled, ShaderCaps.AlphaTestEnabled, ShaderCaps.VertexLighting);
            Shaders("Basic_PositionNormalColorTexture.vs", "Basic_Fragment.frag", ShaderCaps.Spotlight, ShaderCaps.EtEnabled, ShaderCaps.FadeEnabled, ShaderCaps.AlphaTestEnabled, ShaderCaps.VertexLighting);
            Shaders("Basic_PositionNormalTexture.vs", "Basic_Fragment.frag", ShaderCaps.Spotlight, ShaderCaps.EtEnabled, ShaderCaps.FadeEnabled, ShaderCaps.AlphaTestEnabled, ShaderCaps.VertexLighting);
            Shaders("PositionTextureFlip.vs", "DetailMap2Dm1Msk2PassMaterial.frag");
            Shaders("PositionTextureFlip.vs", "DetailMapMaterial.frag");
            Shaders("PositionTextureFlip.vs", "IllumDetailMapMaterial.frag");
            Shaders("PositionTextureFlip.vs", "Masked2DetailMapMaterial.frag");
            Shaders("PositionColorTexture.vs", "Nebula_PositionColorTexture.frag");
            Shaders("Nomad_PositionNormalTexture.vs", "NomadMaterial.frag");
            Shaders("DepthPrepass_Normal.vs", "DepthPrepass_Normal.frag");
            Shaders("DepthPrepass_AlphaTest.vs", "DepthPrepass_AlphaTest.frag");
            Shaders("physicsdebug.vs", "physicsdebug.frag");
            Shaders("AsteroidBand.vs", "AsteroidBand.frag");
            Shaders("Billboard.vs", "sun_radial.frag");
            Shaders("Billboard.vs", "sun_spine.frag");
            Shaders("Billboard.vs", "nebula_extpuff.frag");
        }

        void Shaders(string vert, string frag, params ShaderCaps[] caps)
        {
            foreach(var c in Permute(caps)) {
                ShaderCache.Get(vert, frag, c);
            }
        }
        IEnumerable<ShaderCaps> Permute(ShaderCaps[] caps)
        {
            yield return ShaderCaps.None;
            if (caps == null || caps.Length == 0) yield break;
            var vals = caps.Select((x) => (int)x).ToArray();
            var valsinv = vals.Select(v => ~v).ToArray();
            int max = 0;
            for (int i = 0; i < vals.Length; i++) max |= vals[i];
            for(int i = 0; i <= max; i++) {
                int unaccountedBits = i;
                for(int j = 0;  j < valsinv.Length; j++) {
                    unaccountedBits &= valsinv[j];
                    if(unaccountedBits == 0) {
                        if(i != 0)
                            yield return (ShaderCaps)i;
                        break;
                    }
                }
            }
        }
    }
}

