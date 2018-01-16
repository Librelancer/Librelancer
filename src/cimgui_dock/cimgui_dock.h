#ifndef _CIMGUI_DOCK_H_
#define _CIMGUI_DOCK_H_

#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif
IGEXPORT void igShutdownDock();
IGEXPORT void igBeginDockspace();
IGEXPORT bool igBeginDock(const char *label, bool *opened, int extra_flags);
IGEXPORT void igEndDock();
IGEXPORT void igEndDockspace();
IGEXPORT void igSetDockActive();
IGEXPORT void igLoadDock();
IGEXPORT void igSaveDock();
#ifdef __cplusplus
}
#endif
#endif
