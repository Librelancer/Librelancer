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
namespace LibreLancer
{
    public enum CubeMapFace
    {
        PositiveX,
		NegativeX,
        PositiveY,
		NegativeY,
        PositiveZ,
        NegativeZ
    }
    static class CubeMapFaceExtensions
    {
        public static int ToGL(this CubeMapFace face)
        {
            switch (face)
            {
				case CubeMapFace.PositiveX:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_X;
                case CubeMapFace.PositiveY:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_Y;
                case CubeMapFace.PositiveZ:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_Z;
				case CubeMapFace.NegativeX:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_X;
				case CubeMapFace.NegativeY:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_Y;
				case CubeMapFace.NegativeZ:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_Z;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

