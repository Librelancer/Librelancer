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
using LibreLancer.Fx;

namespace LibreLancer
{
	public class ParticleEffectRenderer : ObjectRenderer
	{
		public float SParam = 0f;
		public bool Active = true;
		SystemRenderer sys;
		ParticleEffectInstance fx;
		public ParticleEffectRenderer(ParticleEffect effect)
		{
			fx = new ParticleEffectInstance(effect);
		}
		public override void Register(SystemRenderer renderer)
		{
			sys = renderer;
			sys.Objects.Add(this);
			fx.Resources = sys.Game.ResourceManager;
		}
		public override void Unregister()
		{
			sys.Objects.Remove(this);
			sys = null;
		}
		Matrix4 tr;
		Vector3 pos;
		float dist = 0;
		const float CULL_DISTANCE = 20000;
		const float CULL = CULL_DISTANCE * CULL_DISTANCE;
		public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
		{
			pos = position;
			if (Active && dist < CULL)
			{
				tr = transform;
				fx.Update(time, transform, SParam);
			}
		}
		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
			dist = VectorMath.DistanceSquared(pos, camera.Position);
			if(Active && dist < (20000 * 20000))
				fx.Draw(sys.Polyline, sys.Game.Billboards, sys.DebugRenderer, tr, SParam);
		}
	}
}
