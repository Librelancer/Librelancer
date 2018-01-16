#include "cimgui_dock.h"
#include "imgui.h"
#include "imgui_dock.h"

IGEXPORT void igShutdownDock()
{
	ImGui::ShutdownDock();
}
IGEXPORT void igRootDock(float posx, float posy, float sizex, float sizey)
{
	ImGui::RootDock(ImVec2(posx, posy), ImVec2(sizex, sizey));
}
IGEXPORT bool igBeginDock(const char *label, bool *opened, int extra_flags)
{
	return ImGui::BeginDock(label, opened, (ImGuiWindowFlags)extra_flags);
}
IGEXPORT void igEndDock()
{
	ImGui::EndDock();
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
IGEXPORT void igPrint()
{
	ImGui::Print();
}
