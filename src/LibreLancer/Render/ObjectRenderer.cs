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
	public abstract class ObjectRenderer
	{
        public string Name;
		public abstract void Update(TimeSpan time, Vector3 position, Matrix4 transform);
		public abstract void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr);
		public virtual void DepthPrepass(ICamera camera, RenderState rstate) { }
		//Rendering Parameters
		public bool LitAmbient = true;
		public bool LitDynamic = true;
		public bool NoFog = false;
		public float[] LODRanges;
        public float InheritCull = float.MinValue;

		public virtual bool OutOfView(ICamera camera)
		{
			return true;
		}

        public virtual bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
		{
            return false;
		}

        public override string ToString()
        {
            return Name ?? GetType().Name;
        }
	}
}

