using System;
namespace LibreLancer
{
    public class MatrixCamera : ICamera
    {
        Matrix4 vp;
        public MatrixCamera(Matrix4 vp)
        {
            this.vp = vp;
        }

        public static Matrix4 CreateTransform(Game game, Rectangle r)
        {
            float gX = (float)game.Width / 2;
            float gY = (float)game.Height / 2;
            var tX = (r.X + (r.Width / 2) - gX) / gX;
            var tY = (gY - r.Y - (r.Height / 2)) / gY;
            var sX = r.Width / (float)(game.Width);
            var sY = r.Height / (float)(game.Height);
            return Matrix4.CreateScale(sX, sY, 1) * Matrix4.CreateTranslation(tX, tY, 0);
        }

        Matrix4 ICamera.ViewProjection => vp;

        Matrix4 ICamera.Projection => Matrix4.Identity;

        Matrix4 ICamera.View => Matrix4.Identity;

        Vector3 ICamera.Position => Vector3.Zero;

        BoundingFrustum ICamera.Frustum => throw new NotImplementedException();

        static long L = 1;
        long ICamera.FrameNumber => L++;
    }
}
