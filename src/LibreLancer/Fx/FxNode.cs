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
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FxNode
	{
		public string Name;
		public string NodeName = "LIBRELANCER:UNNAMED_NODE";
		public float NodeLifeSpan = float.PositiveInfinity;
		public AlchemyTransform Transform;

		public FxNode(AlchemyNode ale)
		{
			Name = ale.Name;
			AleParameter temp;
			if (ale.TryGetParameter ("Node_Name", out temp)) {
				NodeName = (string)temp.Value;
			}
			if (ale.TryGetParameter ("Node_Transform", out temp)) {
				Transform = (AlchemyTransform)temp.Value;
			} else {
				Transform = new AlchemyTransform ();
			}
			if (ale.TryGetParameter ("Node_LifeSpan", out temp)) {
				NodeLifeSpan = (float)temp.Value;
			}
		}
		protected Matrix4 GetTranslation(ParticleEffect effect, Matrix4 attachment, float sparam, float time)
		{
			Matrix4 mat = Transform.GetMatrix (sparam, time);
			if (effect.Parents [this] is FxRootNode) {
				return mat;
			} else if (effect.AttachmentNodes.Contains (this)) {
				return attachment * mat;
			}
			else {
				return effect.Parents [this].GetTranslation (effect, attachment, sparam, time) * mat;
			}
		}
		public FxNode(string name, string nodename)
		{
			Name = name;
			NodeName = nodename;
		}
	}
}

