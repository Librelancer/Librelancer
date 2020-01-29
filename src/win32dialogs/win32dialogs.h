#ifndef _WIN32_DIALOGS_H
#define _WIN32_DIALOGS_H
#define DLGAPI int __declspec(dllexport)

DLGAPI Win32OpenDialog(const char *filters, const char *defaultPath, char **outPath);
DLGAPI Win32SaveDialog(const char *filters, const char *defaultPath, char **outPath);
DLGAPI Win32PickFolder(const char *defaultPath, char **outPath);


#endif