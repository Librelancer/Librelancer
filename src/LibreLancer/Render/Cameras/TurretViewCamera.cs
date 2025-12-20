using System.Numerics;
using LibreLancer.Data.Schema.Cameras;

namespace LibreLancer.Render.Cameras;

public class TurretViewCamera : ICamera
{
    public Rectangle Viewport
    {
        get { return _vp; }
        set
        {
            _vp = value;
            UpdateProjection();
        }
    }

    Rectangle _vp;


    //Camera Values
    public Matrix4x4 Projection { get; private set; }
    public Matrix4x4 View { get; private set; } = Matrix4x4.Identity;
    Matrix4x4 viewprojection;
    bool _vpdirty = true;
    private BoundingFrustum frustum;

    public bool FrustumCheck(BoundingSphere sphere) => frustum.Intersects(sphere);

    public bool FrustumCheck(BoundingBox box) => frustum.Intersects(box);



    void UpdateVp()
    {
        viewprojection = View * Projection;
        frustum = new BoundingFrustum(viewprojection);
        _vpdirty = false;
    }

    public Matrix4x4 ViewProjection
    {
        get
        {
            if (_vpdirty)
            {
                UpdateVp();
            }

            return viewprojection;
        }
    }

    Vector3 _position;

    public Vector3 Position
    {
        get { return _position; }
        set { _position = value; }
    }

    public Vector3 ChasePosition;
    public Vector3 CameraOffset;
    public Vector2 PanControls;
    private Vector2 orbitPan = Vector2.Zero;

    long fnum = 0;


    public void UpdateProjection()
    {
        var aspect = Viewport.AspectRatio;
        var fovV = FOVUtil.CalcFovx(ini.TurretCamera.FovX <= 0 ? 70 : ini.TurretCamera.FovX, aspect);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovV, aspect, 3f, 10000000f);
    }

    private CameraIni ini;

    public TurretViewCamera(Rectangle viewport, CameraIni ini)
    {
        this.ini = ini;
        this.Viewport = viewport;
        ChasePosition = Vector3.Zero;
    }


    public void Update(double delta)
    {
        orbitPan += (PanControls * (float) (delta));

        orbitPan.Y = MathHelper.Clamp(orbitPan.Y,-MathHelper.PiOver2 + 0.02f, MathHelper.PiOver2 - 0.02f);
        var mat = Matrix4x4.CreateFromYawPitchRoll(-orbitPan.X, orbitPan.Y, 0);
        var from = Vector3.Transform(CameraOffset, mat);

        _position = ChasePosition + from;
        View = Matrix4x4.CreateLookAt(ChasePosition + from, ChasePosition, Vector3.UnitY);
        _vpdirty = true;
        fnum++;
    }
}
