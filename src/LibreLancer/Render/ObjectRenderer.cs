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
using OpenTK;
using LibreLancer.GameData;
namespace LibreLancer
{
	public abstract class ObjectRenderer : IDisposable
	{
		protected ICamera camera;
		public Matrix4 World { get; private set; }
		public SystemObject SpaceObject { get; private set; }

		public ObjectRenderer (ICamera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject)
		{
			if (useObjectPosAndRotate)
			{
				World = world * Matrix4.CreateTranslation(spaceObject.Position);
				if(spaceObject.Rotation != null)
					World = spaceObject.Rotation.Value * World;
			}
			else World = Matrix4.Identity;
			SpaceObject = spaceObject;
			this.camera = camera;
		}

		public virtual void Update(TimeSpan elapsed) {}
		public abstract void Draw(RenderState rstate, Lighting lights);
		public abstract void Dispose();

	}
}

