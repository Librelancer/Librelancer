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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
    public interface IDrawable
    {
		void Initialize(ResourceManager cache);
        void Resized();
		void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime);
		void Draw(RenderState rstate, Matrix4 world, Lighting light);
		void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null);
		float GetRadius();
    }
}