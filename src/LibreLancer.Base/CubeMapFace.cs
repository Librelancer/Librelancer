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
using OpenTK.Graphics.OpenGL;
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
    public static class CubeMapFaceExtensions
    {
        public static TextureTarget GL(this CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveX:
                    return TextureTarget.TextureCubeMapPositiveX;
                case CubeMapFace.PositiveY:
                    return TextureTarget.TextureCubeMapPositiveY;
                case CubeMapFace.PositiveZ:
                    return TextureTarget.TextureCubeMapPositiveZ;
                case CubeMapFace.NegativeX:
                    return TextureTarget.TextureCubeMapNegativeX;
                case CubeMapFace.NegativeY:
                    return TextureTarget.TextureCubeMapNegativeY;
                case CubeMapFace.NegativeZ:
                    return TextureTarget.TextureCubeMapNegativeZ;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

