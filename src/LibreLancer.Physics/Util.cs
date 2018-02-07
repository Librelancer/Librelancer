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
using BM = BulletSharp.Math;
namespace LibreLancer.Physics
{
    unsafe static class Util
    {
        public static BM.Matrix Cast(this Matrix4 mat)
        {
            var output = new BM.Matrix();
            *(Matrix4*)&output = mat;
            return output;
        }

        public static Matrix4 Cast(this BM.Matrix mat)
        {
            var output = new Matrix4();
            *(BM.Matrix*)&output = mat;
            return output;
        }

        public static BM.Vector3 Cast(this Vector3 vec)
        {
            var output = new BM.Vector3();
            *(Vector3*)&output = vec;
            return output;
        }

        public static Vector3 Cast(this BM.Vector3 vec)
        {
            var output = new Vector3();
            *(BM.Vector3*)&output = vec;
            return output;
        }


    }
}
