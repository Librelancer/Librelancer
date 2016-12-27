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
	public class IdentityCamera : ICamera
	{
		static IdentityCamera _instance;
		public static IdentityCamera Instance {
			get {
				if (_instance == null)
					_instance = new IdentityCamera ();
				return _instance;
			}
		}
        long _fn = 0;
        public long FrameNumber
        {
            get
            {
                return _fn++;
            }
        }
		public Matrix4 ViewProjection {
			get {
				return Matrix4.Identity;
			}
		}
		public Matrix4 View {
			get {
				return Matrix4.Identity;
			}
		}
		public Matrix4 Projection {
			get {
				return Matrix4.Identity;
			}
		}
		public Vector3 Position {
			get {
				return Vector3.Zero;
			}
		}
		public BoundingFrustum Frustum {
			get {
				return new BoundingFrustum (Matrix4.Identity);
			}
		}
	}
}

