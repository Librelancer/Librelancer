using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.ImUI;

[Flags]
public enum GuizmoOperation : uint
{
    TRANSLATE_X      = (1u << 0),
    TRANSLATE_Y      = (1u << 1),
    TRANSLATE_Z      = (1u << 2),
    ROTATE_X         = (1u << 3),
    ROTATE_Y         = (1u << 4),
    ROTATE_Z         = (1u << 5),
    ROTATE_SCREEN    = (1u << 6),
    SCALE_X          = (1u << 7),
    SCALE_Y          = (1u << 8),
    SCALE_Z          = (1u << 9),
    BOUNDS           = (1u << 10),
    SCALE_XU         = (1u << 11),
    SCALE_YU         = (1u << 12),
    SCALE_ZU         = (1u << 13),

    TRANSLATE = TRANSLATE_X | TRANSLATE_Y | TRANSLATE_Z,
    ROTATE = ROTATE_X | ROTATE_Y | ROTATE_Z | ROTATE_SCREEN,
    SCALE = SCALE_X | SCALE_Y | SCALE_Z,
    SCALEU = SCALE_XU | SCALE_YU | SCALE_ZU, // universal
    UNIVERSAL = TRANSLATE | ROTATE | SCALEU,
    ROTATE_AXIS = ROTATE_X | ROTATE_Y | ROTATE_Z
};

public enum GuizmoMode
{
    LOCAL,
    WORLD
};

public enum GuizmoOp
{
    Nothing,
    Translate,
    Rotate,
    Scale
}

public class ImGuizmo
{
    [DllImport("cimgui", EntryPoint = "igGuizmoBeginFrame")]
    public static extern void BeginFrame();

    [DllImport("cimgui", EntryPoint = "igGuizmoSetOrthographic")]
    public static extern void SetOrthographic();

    [DllImport("cimgui", EntryPoint = "igGuizmoIsUsing")]
    public static extern bool IsUsing();

    [DllImport("cimgui", EntryPoint = "igGuizmoIsOver")]
    public static extern bool IsOver();

    [DllImport("cimgui", EntryPoint = "igGuizmoSetID")]
    public static extern void SetID(int id);

    [DllImport("cimgui", EntryPoint = "igGuizmoSetRect")]
    public static extern void SetRect(float x, float y, float width, float height);

    [DllImport("cimgui", EntryPoint = "igGuizmoManipulate")]
    static unsafe extern GuizmoOp igGuizmoManipulate(ref Matrix4x4 view, ref Matrix4x4 projection, GuizmoOperation operation,
        GuizmoMode mode, Matrix4x4* matrix, Matrix4x4* delta);

    //Precision workaround - hacky
    public static Matrix4x4 ApplyDelta(Matrix4x4 matrix, Matrix4x4 delta, GuizmoOp op)
    {
        switch (op)
        {
            case GuizmoOp.Nothing:
                return matrix;
            case GuizmoOp.Scale:
            case GuizmoOp.Translate:
                return matrix * delta;
            case GuizmoOp.Rotate:
                var rot = (matrix * delta).ExtractRotation();
                return Matrix4x4.CreateFromQuaternion(rot) *
                       Matrix4x4.CreateTranslation(Vector3.Transform(Vector3.Zero, matrix));
            default:
                throw new InvalidOperationException();
        }
    }


    public static unsafe GuizmoOp Manipulate(ref Matrix4x4 view, ref Matrix4x4 projection,
        GuizmoOperation operation, GuizmoMode mode, ref Matrix4x4 matrix, out Matrix4x4 delta)
    {
        fixed (Matrix4x4* m = &matrix, d = &delta)
        {
            return igGuizmoManipulate(ref view, ref projection, operation, mode, m, d);
        }
    }

    [DllImport("cimgui", EntryPoint = "igGuizmoSetDrawlist")]
    public static extern void SetDrawlist();
}
