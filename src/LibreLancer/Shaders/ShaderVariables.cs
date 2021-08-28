// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Shaders
{
	public class ShaderVariables
	{
		int viewPosition;
        int projectionPosition;
		int viewProjectionPosition;
		int worldPosition;
		int normalMatrixPosition;

		int dtSamplerPosition;
		int dm1SamplerPosition;
		int dm0SamplerPosition;
		int dmSamplerPosition;
		int acPosition;
		int dcPosition;
		int ocPosition;
		int ecPosition;
		int etSamplerPosition;

		int flipUPosition;
		int flipVPosition;
		int tileRatePosition;
		int tileRate0Position;
		int tileRate1Position;

		int lightParametersPosition;
		int ambientColorPosition;
		int lightsPosition;
        int spotlightParamsPosition;

		int fogColorPosition;
		int fogRangePosition;

		int fadeRangePosition;

		int materialAnimPosition;
        int skinningEnabledPosition;
		Shader shader;

		public ShaderVariables(Shader sh)
		{
			shader = sh;

			viewPosition = sh.GetLocation("View");
			viewProjectionPosition = sh.GetLocation("ViewProjection");
            projectionPosition = sh.GetLocation("Projection");
			worldPosition = sh.GetLocation("World");
			normalMatrixPosition = sh.GetLocation("NormalMatrix");

			dtSamplerPosition = sh.GetLocation("DtSampler");
			dm1SamplerPosition = sh.GetLocation("Dm1Sampler");
			dm0SamplerPosition = sh.GetLocation("Dm0Sampler");
			dmSamplerPosition = sh.GetLocation("DmSampler");
			dcPosition = sh.GetLocation("Dc");
			acPosition = sh.GetLocation("Ac");
			ocPosition = sh.GetLocation("Oc");
			ecPosition = sh.GetLocation("Ec");
			etSamplerPosition = sh.GetLocation("EtSampler");

			flipUPosition = sh.GetLocation("FlipU");
			flipVPosition = sh.GetLocation("FlipV");
			tileRatePosition = sh.GetLocation("TileRate");
			tileRate0Position = sh.GetLocation("TileRate0");
			tileRate1Position = sh.GetLocation("TileRate1");

			lightParametersPosition = sh.GetLocation("LightingParameters");
			ambientColorPosition = sh.GetLocation("AmbientColor");
            lightsPosition = sh.GetLocation("LightData");
			spotlightParamsPosition = sh.GetLocation("SpotlightData");

			fogColorPosition = sh.GetLocation("FogColor");
			fogRangePosition = sh.GetLocation("FogRange");

			fadeRangePosition = sh.GetLocation("FadeRange");
			materialAnimPosition = sh.GetLocation("MaterialAnim");
            skinningEnabledPosition = sh.GetLocation("SkinningEnabled");
		}

		public void UseProgram()
		{
			shader.UseProgram();
		}
        public Shader Shader
		{
			get
			{
				return shader;
			}
		}

        public void SetSkinningEnabled(bool skinningEnabled)
        {
            if (skinningEnabledPosition != -1)
                shader.SetInteger(skinningEnabledPosition, skinningEnabled ? 1 : 0);
        }

        public void SetView(ref Matrix4x4 view)
		{
			if (viewPosition != -1)
				shader.SetMatrix(viewPosition, ref view);
            _camera = null;
		}

		public void SetViewProjection(ref Matrix4x4 viewProjection)
		{
			if (viewProjectionPosition != -1)
				shader.SetMatrix(viewProjectionPosition, ref viewProjection);
            _camera = null;
        }

        //Set View and ViewProjection once per frame per shader
        ICamera _camera;
        long _vframeNumber;
        long _vpframeNumber;
        long _pframeNumber;

        public void SetView(ICamera camera)
        {
            if (camera == _camera && camera.FrameNumber == _vframeNumber)
                return;
            _camera = camera;
            _vframeNumber = camera.FrameNumber;
            var v = camera.View;
            if (viewPosition != -1)
                shader.SetMatrix(viewPosition, ref v);
        }

        private int lightId;
        private long lightFrame;
        private int nebulaId;
        private BitArray128 lightsEnabled;
        private int ltcnt;
        public bool NeedLightData(ref Lighting lights, long frameNumber, out int lightCount)
        {
            var retval = lights.Lights.SourceLighting.ID != lightId ||
                   lightFrame != frameNumber ||
                   nebulaId != lights.Lights.NebulaCount ||
                   lightsEnabled != lights.Lights.SourceEnabled;
            lightCount = retval ? 0 : ltcnt;
            return retval;
        }
        public void UpdateLightDataCheck(ref Lighting lights, long frameNumber, int lightCount)
        {
            lightId = lights.Lights.SourceLighting.ID;
            lightFrame = frameNumber;
            lightsEnabled = lights.Lights.SourceEnabled;
            nebulaId = lights.Lights.NebulaCount;
            ltcnt = lightCount;
        }
        public void SetProjection(ICamera camera)
        {
            if (camera == _camera && camera.FrameNumber == _pframeNumber)
                return;
            _camera = camera;
            _pframeNumber = camera.FrameNumber;
            var v = camera.Projection;
            if (projectionPosition != -1)
                shader.SetMatrix(projectionPosition, ref v);
        }

        public void SetViewProjection(ICamera camera)
        {
            if (camera == _camera && camera.FrameNumber == _vpframeNumber)
                return;
            _camera = camera;
            _vpframeNumber = camera.FrameNumber;
            var vp = camera.ViewProjection;
            if (viewProjectionPosition != -1)
                shader.SetMatrix(viewProjectionPosition, ref vp);
        }

		public unsafe void SetWorld(WorldMatrixHandle world)
		{
            if (world.Source == (Matrix4x4*) 0)
            {
                var id = Matrix4x4.Identity;
                if (worldPosition != -1)
                    shader.SetMatrix(worldPosition, ref id);
                if (normalMatrixPosition != -1)
                    shader.SetMatrix(normalMatrixPosition, ref id);
            }
            else if (world.ID == ulong.MaxValue || shader.UserTag != world.ID)
            {
                shader.UserTag = world.ID;
                if (worldPosition != -1)
                    shader.SetMatrix(worldPosition, (IntPtr) world.Source);
                if (normalMatrixPosition != -1)
                    shader.SetMatrix(normalMatrixPosition, (IntPtr) (&world.Source[1]));
            }
        }
        public void SetWorld(ref Matrix4x4 world, ref Matrix4x4 normal)
        {
            shader.UserTag = 0;
            if (worldPosition != -1)
                shader.SetMatrix(worldPosition, ref world);
            if (normalMatrixPosition != -1)
                shader.SetMatrix(normalMatrixPosition, ref normal);
        }
        public void SetAc(Color4 ac)
		{
			if (acPosition != -1)
				shader.SetColor4(acPosition, ac);
		}

		public void SetDc(Color4 dc)
		{
			if (dcPosition != -1)
				shader.SetColor4(dcPosition, dc);
		}

		public void SetDtSampler(int dt)
		{
			if (dtSamplerPosition != -1)
				shader.SetInteger(dtSamplerPosition, dt);
		}

		public void SetDm1Sampler(int dt)
		{
			if (dm1SamplerPosition != -1)
				shader.SetInteger(dm1SamplerPosition, dt);
		}

		public void SetDm0Sampler(int dt)
		{
			if (dm0SamplerPosition != -1)
				shader.SetInteger(dm0SamplerPosition, dt);
		}

		public void SetDmSampler(int dt)
		{
			if (dmSamplerPosition != -1)
				shader.SetInteger(dmSamplerPosition, dt);
		}

		public void SetOc(float oc)
		{
			if (ocPosition != -1)
				shader.SetFloat(ocPosition, oc);
		}

		public void SetEc(Color4 ec)
		{
			if (ecPosition != -1)
				shader.SetColor4(ecPosition, ec);
		}

		public void SetEtSampler(int et)
		{
			if (etSamplerPosition != -1)
				shader.SetInteger(etSamplerPosition, et);
		}

		public void SetFlipU(int flip)
		{
			if (flipUPosition != -1)
				shader.SetInteger(flipUPosition, flip);
		}

		public void SetFlipV(int flip)
		{
			if (flipVPosition != -1)
				shader.SetInteger(flipVPosition, flip);
		}

		public void SetTileRate(float rate)
		{
			if (tileRatePosition != -1)
				shader.SetFloat(tileRatePosition, rate);
		}

		public void SetTileRate0(float rate)
		{
			if (tileRate0Position != -1)
				shader.SetFloat(tileRate0Position, rate);
		}

		public void SetTileRate1(float rate)
		{
			if (tileRate1Position != -1)
				shader.SetFloat(tileRate1Position, rate);
		}

        public void SetLightParameters(Vector4i parameters)
		{
            if (lightParametersPosition != -1)
                shader.SetVector4i(lightParametersPosition, parameters);
		}

		public void SetAmbientColor(Color4 ambient)
		{
			if (ambientColorPosition != -1)
				shader.SetColor4(ambientColorPosition, ambient);
		}

		public unsafe void SetLightData(Vector4 *positions, int count)
        {
            if (lightsPosition != -1)
                shader.SetVector4Array(lightsPosition, positions, count);
        }
        public unsafe void SetSpotlightData(Vector4* param, int count)
		{
			if (spotlightParamsPosition != -1)
				shader.SetVector4Array(spotlightParamsPosition, param, count);
		}

		public void SetFogColor(Color4 color)
		{
			if (fogColorPosition != -1)
				shader.SetColor4(fogColorPosition, color);
		}

		public void SetFogRange(Vector2 range)
		{
			if (fogRangePosition != -1)
				shader.SetVector2(fogRangePosition, range);
		}

		public void SetFadeRange(Vector2 range)
		{
			if (fadeRangePosition != -1)
				shader.SetVector2(fadeRangePosition, range);
		}

		public void SetMaterialAnim(Vector4 anim)
		{
			if (materialAnimPosition != -1)
				shader.SetVector4(materialAnimPosition, anim);
		}

        public static void Log(string text)
        {
            FLLog.Debug("Shader", text);
        }

        public static ShaderVariables Compile(string vertex, string fragment, string insert)
        {
            string prelude;
            if (GLExtensions.Features430)
                prelude = "#version 430\n#define FEATURES430\n" + insert;
            else if (GL.GLES)
                prelude = "#version 310 es\nprecision highp float;\nprecision highp int;\n"  + insert;
            else
                prelude = "#version 150\n" + insert;
            return new ShaderVariables(new Shader(prelude + vertex, prelude + fragment));
        }
	}
}
