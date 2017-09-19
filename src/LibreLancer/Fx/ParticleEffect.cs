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
using System.Linq;
namespace LibreLancer.Fx
{
	public class ParticleEffect
	{
		ParticleLibrary lib;

		public string Name;
		public uint CRC;
		public Dictionary<FxNode, FxNode> Parents = new Dictionary<FxNode, FxNode>();
		public Dictionary<FxNode, List<FxNode>> Pairs = new Dictionary<FxNode, List<FxNode>>();
		public List<FxNode> AttachmentNodes = new List<FxNode>();
		List<FxEmitter> emitters = new List<FxEmitter>();
		public List<FxNode> Nodes;

		public int EmitterCount
		{
			get
			{
				return emitters.Count;
			}
		}
		public ParticleEffect (ParticleLibrary lib)
		{
			this.lib = lib;
		}

		public ResourceManager ResourceManager
		{	
			get
			{
				return lib.Resources;
			}
		}

		public void SetNodes(IEnumerable<FxNode> nodes)
		{
			Nodes = nodes.ToList();
			foreach (var n in Nodes)
			{
				if (n is FxEmitter)
					emitters.Add((FxEmitter)n);
			}
		}

		public void Update(ParticleEffectInstance instance, TimeSpan delta, ref Matrix4 transform, float sparam)
		{
			for (int i = 0; i < emitters.Count; i++)
			{
				emitters[i].Update(this, instance, delta, ref transform, sparam);
			}
		}
	}
}

