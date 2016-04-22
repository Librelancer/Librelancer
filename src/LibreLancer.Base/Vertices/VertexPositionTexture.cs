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
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionTexture : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public VertexPositionTexture(Vector3 pos, Vector2 texcoord)
        {
            Position = pos;
            TextureCoordinate = texcoord;
        }
		public void SetVertexPointers(int offset)
        {
            GL.EnableVertexAttribArray(VertexSlots.Position);
            GL.EnableVertexAttribArray(VertexSlots.Texture1);
            GL.VertexAttribPointer(VertexSlots.Position, 3, VertexAttribPointerType.Float, false, VertexSize(), offset + 0);
            GL.VertexAttribPointer(VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 3);
        }
        public int VertexSize()
        {
            return sizeof(float) * 3 + sizeof(float) * 2;
        }
    }
}