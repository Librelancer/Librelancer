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
    public enum SurfaceFormat
    {
        Color,
        Bgr565,
        Bgra5551,
        Bgra4444,
        Dxt1,
        Dxt3,
        Dxt5,
        NormalizedByte2,
        NormalizedByte4,
        Rgba1010102,
        Rg32,
        Rgba64,
        //Alpha8, - Removed in OpenGL 3.1,
		R8, //Just red channel - NOT an XNA value
		Depth, //Depth texture - NOT an XNA value
        Single,
        Vector2,
        Vector4,
        HalfSingle,
        HalfVector2,
        HalfVector4,
        HdrBlendable
    }
}

