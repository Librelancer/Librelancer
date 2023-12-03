#ifndef _WIN32_DIALOGS_H
#define _WIN32_DIALOGS_H
#define DLGAPI int __declspec(dllexport)
#include <wchar.h>
DLGAPI Win32CrashDialog(const wchar_t *title, const wchar_t *message, const wchar_t *details);
#endif
