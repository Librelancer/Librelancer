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
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosition : IVertexType
    {
        public Vector3 Position;

        public VertexPosition(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public VertexPosition(Vector3 position)
            : this()
        {
            this.Position = position;
        }

		public VertexDeclaration GetVertexDeclaration()
		{
			return new VertexDeclaration(
				sizeof(float) * 3,
				new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0)
			);
		}
    }
}
