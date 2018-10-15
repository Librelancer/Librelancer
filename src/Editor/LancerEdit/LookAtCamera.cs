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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer;
namespace LancerEdit
{
    public class LookAtCamera : ICamera
    {
        Matrix4 view;
        Matrix4 projection;
        Matrix4 vp;
        Vector3 pos;
        public void Update(float vw, float vh, Vector3 from, Vector3 to, Matrix4? rot = null)
        {
            pos = from;
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(50), vw / vh, 0.1f, 300000);
            var up = (rot ?? Matrix4.Identity).Transform(-Vector3.Up);
            view = Matrix4.LookAt(from, to, up);
            vp = view * projection;
        }
        public Matrix4 ViewProjection {
            get {
                return vp;
            }
        }

        public Matrix4 Projection {
            get {
                return projection;
            }
        }

        public Matrix4 View {
            get {
                return view;
            }
        }

        public Vector3 Position {
            get {
                return pos;
            }
        }

        public BoundingFrustum Frustum {
            get {
                return new BoundingFrustum(vp);
            }
        }

        long fn = 0;
        public long FrameNumber {
            get {
                return fn++;
            }
        }
    }
}
