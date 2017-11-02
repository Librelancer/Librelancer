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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
using LibreLancer.Utf.Dfm;
namespace LibreLancer
{
	public class CharacterRenderer : ObjectRenderer
	{
		public DfmFile Head;
		public DfmFile Body;
		public DfmFile LeftHand;
		public DfmFile RightHand;

		public CharacterRenderer(DfmFile head, DfmFile body, DfmFile leftHand, DfmFile rightHand)
		{
			Head = head;
			Body = body;
			LeftHand = leftHand;
			RightHand = rightHand;
		}

		Matrix4 transform;
		public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
		{
			this.transform = transform;
		}

		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
			Body.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var bhps = Body.GetHardpoints().Where((arg) => arg.Hp.Name.ToLowerInvariant() == "hp_head").First();
			Body.DrawBuffer(commands, transform, Lighting.Empty);
			Head.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			Head.DrawBuffer(commands, bhps.GetTransform(transform), Lighting.Empty);
			var hhps = Head.GetHardpoints().ToArray();
			LeftHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var lhhps = LeftHand.GetHardpoints().ToArray();
			RightHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var rhhps = RightHand.GetHardpoints().ToArray();
		}

		SystemRenderer sysr;
		public override void Register(SystemRenderer renderer)
		{
			sysr = renderer;
			sysr.Objects.Add(this);
		}

		public override void Unregister()
		{
			sysr.Objects.Remove(this);
			sysr = null;
		}
	}
}
