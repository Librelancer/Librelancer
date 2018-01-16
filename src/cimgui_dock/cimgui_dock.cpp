#include "cimgui_dock.h"
#include "imgui.h"
#include "imgui_dock.h"

IGEXPORT void igShutdownDock()
{
	ImGui::ShutdownDock();
}
IGEXPORT void igBeginDockspace()
{
	ImGui::BeginDockspace();
}
IGEXPORT bool igBeginDock(const char *label, bool *opened, int extra_flags)
{
	return ImGui::BeginDock(label, opened, (ImGuiWindowFlags)extra_flags);
}
IGEXPORT void igEndDock()
{
	ImGui::EndDock();
}
IGEXPORT void igEndDockspace()
{
	ImGui::EndDockspace();
}
IGEXPORT void igSetDockActive()
{
	ImGui::SetDockActive();
}
IGEXPORT void igLoadDock()
{
	ImGui::LoadDock();
}
IGEXPORT void igSaveDock()
{
	ImGui::SaveDock();
}
