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
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	public class Hardpoint
	{
		Matrix4 transform;
		AbstractConstruct parent;
		public string Name;
		public Hardpoint(HardpointDefinition def, AbstractConstruct parent)
		{
			this.parent = parent;
			this.transform = def.Transform;
			Name = def.Name;
            IsStatic = parent is FixConstruct && def is FixedHardpointDefinition;
		}
        public bool IsStatic { get; private set; }
		public Matrix4 Transform
		{
			get
			{
				if (parent != null)
					return transform * parent.Transform;
				else
					return transform;
			}
		}
		public override string ToString()
		{
			return string.Format("[{0}, IsStatic={1}]", Name, IsStatic);
		}
	}
}

