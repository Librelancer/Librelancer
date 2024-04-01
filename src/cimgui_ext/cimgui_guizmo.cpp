#include "imgui.h"
#include "cimgui_ext.h"
#include "ImGuizmo.h"

IGEXPORT void igGuizmoBeginFrame()
{
    ImGuizmo::BeginFrame();
}

IGEXPORT void igGuizmoSetOrthographic(int orthographic)
{
    ImGuizmo::SetOrthographic(orthographic != 0);
}

IGEXPORT int igGuizmoIsUsing()
{
    return ImGuizmo::IsUsing() ? 1 : 0;
}

IGEXPORT int igGuizmoIsOver()
{
    return ImGuizmo::IsOver() ? 1 : 0;
}

IGEXPORT void igGuizmoSetID(int id)
{
    ImGuizmo::SetID(id);
}

IGEXPORT void igGuizmoSetRect(float x, float y, float width, float height)
{
    ImGuizmo::SetRect(x, y, width, height);
}

IGEXPORT int igGuizmoManipulate(float* view, float* projection, int operation, int mode, float* matrix, float* delta)
{
    return ImGuizmo::Manipulate(view, projection, (ImGuizmo::OPERATION)operation, (ImGuizmo::MODE)mode, matrix, delta, NULL, NULL, NULL);
}

IGEXPORT void igGuizmoSetDrawlist()
{
    ImGuizmo::SetDrawlist();
}

IGEXPORT void igGuizmoSetImGuiContext(void* ctx)
{
    ImGuizmo::SetImGuiContext((ImGuiContext*)ctx);
}
