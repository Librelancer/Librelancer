#include "cimgui_dock.h"
#include "imgui.h"
#include "imgui_dock.h"

void igShutdownDock()
{
	ImGui::ShutdownDock();
}
void igRootDock(float posx, float posy, float sizex, float sizey)
{
	ImGui::RootDock(ImVec2(posx, posy), ImVec2(sizex, sizey));
}
bool igBeginDock(const char *label, bool *opened, int extra_flags)
{
	return ImGui::BeginDock(label, opened, (ImGuiWindowFlags)extra_flags);
}
void igEndDock()
{
	ImGui::EndDock();
}
void igSetDockActive()
{
	ImGui::SetDockActive();
}
void igLoadDock()
{
	ImGui::LoadDock();
}
void igSaveDock()
{
	ImGui::SaveDock();
}
void igPrint()
{
	ImGui::Print();
}
