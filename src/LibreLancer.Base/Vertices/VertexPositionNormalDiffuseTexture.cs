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
    public struct VertexPositionNormalDiffuseTexture
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Diffuse;
        public Vector2 TextureCoordinate;

        public VertexPositionNormalDiffuseTexture(BinaryReader reader)
            : this()
        {
            this.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            this.Normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            //this.Diffuse = reader.ReadUInt32();
			Diffuse = new Color4(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f);
			this.TextureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
        }
    }
}