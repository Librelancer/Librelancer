#include "win32dialogs.h"
#include <nfd.h>

DLGAPI Win32OpenDialog(const char *filters, const char *defaultPath, char **outPath)
{
	return NFD_OpenDialog(filters, defaultPath, outPath) == NFD_OKAY;
}

DLGAPI Win32SaveDialog(const char *filters, const char *defaultPath, char **outPath)
{
	return NFD_SaveDialog(filters, defaultPath, outPath) == NFD_OKAY;
}

DLGAPI Win32PickFolder(const char *defaultPath, char **outPath)
{
	return NFD_PickFolder(defaultPath, outPath) == NFD_OKAY;
}