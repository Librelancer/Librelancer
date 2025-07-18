#include "imgui.h"
#include "cimgui_ext.h"
#include "ImGuizmo.h"

CIMGUI_API void igGuizmoBeginFrame()
{
    ImGuizmo::BeginFrame();
}

CIMGUI_API void igGuizmoSetOrthographic(int orthographic)
{
    ImGuizmo::SetOrthographic(orthographic != 0);
}

CIMGUI_API int igGuizmoIsUsing()
{
    return ImGuizmo::IsUsing() ? 1 : 0;
}

CIMGUI_API int igGuizmoIsOver()
{
    return ImGuizmo::IsOver() ? 1 : 0;
}

CIMGUI_API void igGuizmoSetID(int id)
{
    ImGuizmo::SetID(id);
}

CIMGUI_API void igGuizmoSetRect(float x, float y, float width, float height)
{
    ImGuizmo::SetRect(x, y, width, height);
}

CIMGUI_API int igGuizmoManipulate(float* view, float* projection, int operation, int mode, float* matrix, float* delta)
{
    return ImGuizmo::Manipulate(view, projection, (ImGuizmo::OPERATION)operation, (ImGuizmo::MODE)mode, matrix, delta, NULL, NULL, NULL);
}

CIMGUI_API void igGuizmoSetDrawlist()
{
    ImGuizmo::SetDrawlist();
}

CIMGUI_API void igGuizmoSetImGuiContext(void* ctx)
{
    ImGuizmo::SetImGuiContext((ImGuiContext*)ctx);
}
