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
namespace LibreLancer
{
	public class ShaderVariables
	{
		int viewPosition;
		int viewProjectionPosition;
		int worldPosition;
		int normalMatrixPosition;


		int dtSamplerPosition;
		int dm1SamplerPosition;
		int dm0SamplerPosition;
		int dmSamplerPosition;
		int acPosition;
		int dcPosition;
		int ocEnabledPosition;
		int ocPosition;
		int ecPosition;
		int etSamplerPosition;

		int flipUPosition;
		int flipVPosition;
		int tileRatePosition;
		int tileRate0Position;
		int tileRate1Position;

		int lightingEnabledPosition;
		int ambientColorPosition;
		int lightCountPosition;
		int lightsPosPosition;
		int lightsDirPosition;
		int lightsColorPosition;
		int lightsAttenuationPosition;
		int lightsRangePosition;

		int fogEnabledPosition;
		int fogColorPosition;
		int fogRangePosition;

		Shader shader;

		public ShaderVariables(Shader sh)
		{
			shader = sh;

			viewPosition = sh.GetLocation("View");
			viewProjectionPosition = sh.GetLocation("ViewProjection");
			worldPosition = sh.GetLocation("World");
			normalMatrixPosition = sh.GetLocation("NormalMatrix");

			dtSamplerPosition = sh.GetLocation("DtSampler");
			dm1SamplerPosition = sh.GetLocation("Dm1Sampler");
			dm0SamplerPosition = sh.GetLocation("Dm0Sampler");
			dmSamplerPosition = sh.GetLocation("DmSampler");
			dcPosition = sh.GetLocation("Dc");
			acPosition = sh.GetLocation("Ac");
			ocEnabledPosition = sh.GetLocation("OcEnabled");
			ocPosition = sh.GetLocation("Oc");
			ecPosition = sh.GetLocation("Ec");
			etSamplerPosition = sh.GetLocation("EtSampler");

			flipUPosition = sh.GetLocation("FlipU");
			flipVPosition = sh.GetLocation("FlipV");
			tileRatePosition = sh.GetLocation("TileRate");
			tileRate0Position = sh.GetLocation("TileRate0");
			tileRate1Position = sh.GetLocation("TileRate1");

			lightingEnabledPosition = sh.GetLocation("LightingEnabled");
			ambientColorPosition = sh.GetLocation("AmbientColor");
			lightCountPosition = sh.GetLocation("LightCount");
			lightsPosPosition = sh.GetLocation("LightsPos");
			lightsDirPosition = sh.GetLocation("LightsDir");
			lightsColorPosition = sh.GetLocation("LightsColor");
			lightsAttenuationPosition = sh.GetLocation("LightsAttenuation");
			lightsRangePosition = sh.GetLocation("LightsRange");

			fogEnabledPosition = sh.GetLocation("FogEnabled");
			fogColorPosition = sh.GetLocation("FogColor");
			fogRangePosition = sh.GetLocation("FogRange");
		}

		public void UseProgram()
		{
			shader.UseProgram();
		}

		public int UserTag
		{
			get {
				return shader.UserTag;
			} set {
				shader.UserTag = value;
			}
		}

		public Shader Shader
		{
			get
			{
				return shader;
			}
		}
		public void SetView(ref Matrix4 view)
		{
			if (viewPosition != -1)
				shader.SetMatrix(viewPosition, ref view);
            _camera = null;
		}

		public void SetViewProjection(ref Matrix4 viewProjection)
		{
			if (viewProjectionPosition != -1)
				shader.SetMatrix(viewProjectionPosition, ref viewProjection);
            _camera = null;
        }

        //Set View and ViewProjection once per frame per shader
        ICamera _camera;
        long _vframeNumber;
        long _vpframeNumber;

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

		public void SetWorld(ref Matrix4 world)
		{
			if (worldPosition != -1)
				shader.SetMatrix(worldPosition, ref world);
		}

		public void SetNormalMatrix(ref Matrix4 normal)
		{
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

		public void SetOcEnabled(int oce)
		{
			if (ocEnabledPosition != -1)
				shader.SetInteger(ocEnabledPosition, oce);
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

		public void SetLightingEnabled(int enabled)
		{
			if (lightingEnabledPosition != -1)
				shader.SetInteger(lightingEnabledPosition, enabled);
		}

		public void SetAmbientColor(Color4 ambient)
		{
			if (ambientColorPosition != -1)
				shader.SetColor4(ambientColorPosition, ambient);
		}

		public void SetLightCount(int count)
		{
			if (lightCountPosition != -1)
				shader.SetInteger(lightCountPosition, count);
		}

		public void SetLightsPos(int index, Vector4 pos)
		{
			if (lightsPosPosition != -1)
				shader.SetVector4(lightsPosPosition, pos, index);
		}

		public void SetLightsDir(int index, Vector3 dir)
		{
			if (lightsDirPosition != -1)
				shader.SetVector3(lightsDirPosition, dir, index);
		}

		public void SetLightsColor(int index, Vector3 color)
		{
			if (lightsColorPosition != -1)
				shader.SetVector3(lightsColorPosition, color, index);
		}

		public void SetLightsAttenuation(int index, Vector4 attenuation)
		{
			if (lightsAttenuationPosition != -1)
				shader.SetVector4(lightsAttenuationPosition, attenuation, index);
		}

		public void SetLightsRange(int index, int range)
		{
			if (lightsRangePosition != -1)
				shader.SetInteger(lightsRangePosition, range, index);
		}

		public void SetFogEnabled(int enabled)
		{
			if (fogEnabledPosition != -1)
				shader.SetInteger(fogEnabledPosition, enabled);
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
	}
}
