#ifndef _CIMGUI_SPINNER_H_
#define _CIMGUI_SPINNER_H_

#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif 
IGEXPORT bool igExtSpinner(const char* label, float radius, int thickness, int color);
#ifdef __cplusplus
}
#endif
#endif
