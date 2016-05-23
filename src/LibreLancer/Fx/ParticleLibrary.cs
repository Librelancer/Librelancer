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
using System.Collections.Generic;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class ParticleLibrary
	{
		public List<FxNode> Nodes = new List<FxNode> ();
		public List<ParticleEffect> Effects = new List<ParticleEffect>();
		public ResourceManager Resources;

		public ParticleLibrary (ResourceManager res, AleFile ale)
		{
			Resources = res;
			foreach (var node in ale.NodeLib.Nodes) {
				Nodes.Add (NodeFromAle (node));
			}
			foreach (var effect in ale.FxLib.Effects) {
				var fx = new ParticleEffect (this);
				fx.CRC = effect.CRC;
				fx.Name = effect.Name;

				foreach (var noderef in effect.Fx) {
					if (noderef.IsAttachmentNode)
						fx.AttachmentNodes.Add (Nodes [(int)noderef.Index]);
				}

			}
		}
		static FxNode NodeFromAle(AlchemyNode ale)
		{
			switch (ale.Name) {
			case "FxBasicAppearance":
				return new FxBasicAppearance (ale);
			case "FxConeEmitter":
				return new FxConeEmitter (ale);
			default:
				throw new NotImplementedException (ale.Name);
			}
		}
	}
}

