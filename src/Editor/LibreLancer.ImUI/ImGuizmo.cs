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
    public static unsafe extern bool Manipulate(ref Matrix4x4 view, ref Matrix4x4 projection, GuizmoOperation operation,
        GuizmoMode mode, Matrix4x4* matrix, Matrix4x4* delta);

    [DllImport("cimgui", EntryPoint = "igGuizmoSetDrawlist")]
    public static extern void SetDrawlist();
}