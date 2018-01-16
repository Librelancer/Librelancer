#ifndef _CIMGUI_DOCK_H_
#define _CIMGUI_DOCK_H_

#ifdef __cplusplus
extern "C" {
#endif

void igShutdownDock();
void igRootDock(float posx, float posy, float sizex, float sizey);
bool igBeginDock(const char *label, bool *opened, int extra_flags);
void igEndDock();
void igSetDockActive();
void igLoadDock();
void igSaveDock();
void igPrint();
#ifdef __cplusplus
}
#endif
#endif
