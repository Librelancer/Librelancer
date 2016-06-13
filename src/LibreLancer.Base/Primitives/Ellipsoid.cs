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
 * The Original Code is RenderTools code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using LibreLancer.Vertices;

namespace LibreLancer.Primitives
{
    public class Ellipsoid : IDisposable
    {
        public VertexBuffer VertexBuffer { get; private set; }
        public ElementBuffer ElementBuffer { get; private set; }

        public Ellipsoid(Vector3 Size, int stacks, int slices)
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[(slices + 1) * (stacks + 1)];
            ushort[] indices = new ushort[slices * stacks * 6];

            float deltaTheta = MathHelper.Pi / stacks;
            float deltaPhi = MathHelper.TwoPi / slices;
            ushort vertexIndex = 0, index = 0;
            ushort sliceCount = (ushort)(slices + 1);

            for (ushort stack = 0; stack <= stacks; stack++)
            {
                float theta = MathHelper.PiOver2 - stack * deltaTheta;
                float y = Size.Y * (float)Math.Sin(theta);

                for (ushort slice = 0; slice <= slices; slice++)
                {
                    float phi = slice * deltaPhi;
                    float x = -Size.X * (float)Math.Cos(theta) * (float)Math.Sin(phi);
                    float z = -Size.Z * (float)Math.Cos(theta) * (float)Math.Cos(phi);

                    Vector3 pos = new Vector3(x, y, z);
                    vertices[vertexIndex++] = new VertexPositionTexture(
                        pos,
                        new Vector2((float)slice / (float)slices, (float)stack / (float)stacks)
                    );

                    if (stack < stacks && slice < slices)
                    {
                        indices[index++] = (ushort)((stack + 0) * sliceCount + slice);
                        indices[index++] = (ushort)((stack + 1) * sliceCount + slice);
                        indices[index++] = (ushort)((stack + 0) * sliceCount + slice + 1);

                        indices[index++] = (ushort)((stack + 0) * sliceCount + slice + 1);
                        indices[index++] = (ushort)((stack + 1) * sliceCount + slice);
                        indices[index++] = (ushort)((stack + 1) * sliceCount + slice + 1);
                    }
                }
            }

            VertexBuffer = new VertexBuffer(typeof(VertexPositionTexture), vertices.Length);
            VertexBuffer.SetData<VertexPositionTexture>(vertices);

            ElementBuffer = new ElementBuffer(indices.Length);
            ElementBuffer.SetData(indices);
            VertexBuffer.SetElementBuffer(ElementBuffer);
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            ElementBuffer.Dispose();
        }
    }
}
