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
namespace LibreLancer
{
    public class MatrixCamera : ICamera
    {
        public Matrix4 Matrix;

        public MatrixCamera(Matrix4 vp)
        {
            this.Matrix = vp;
        }

        int w = -1, h = -1;
        Rectangle rect;
        public void CreateTransform(Game game, Rectangle r)
        {
            if (w == game.Width && h == game.Height && rect == r)
                return;
            w = game.Width;
            h = game.Height;
            rect = r;
            float gX = (float)game.Width / 2;
            float gY = (float)game.Height / 2;
            var tX = (r.X + (r.Width / 2) - gX) / gX;
            var tY = (gY - r.Y - (r.Height / 2)) / gY;
            var sX = r.Width / (float)(game.Width);
            var sY = r.Height / (float)(game.Height);
            Matrix = Matrix4.CreateScale(sX, sY, 1) * Matrix4.CreateTranslation(tX, tY, 0);
        }

        Matrix4 ICamera.ViewProjection => Matrix;

        Matrix4 ICamera.Projection => Matrix4.Identity;

        Matrix4 ICamera.View => Matrix4.Identity;

        Vector3 ICamera.Position => Vector3.Zero;

        BoundingFrustum ICamera.Frustum => throw new NotImplementedException();

        static long L = 1;
        long ICamera.FrameNumber => L++;
    }
}
