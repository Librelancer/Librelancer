#include "win32dialogs.h"
#include <nfd.h>
#include "combaseapi.h"
#include <stdlib.h>
#include <string.h>

// Copy from the allocator used from nfd to something .NET can free
void CopyToWin32Memory(char *input, char **output)
{
    if(input) 
    {
        int len = strlen(input);
        *output = (char*)CoTaskMemAlloc(len + 1);
        (*output)[len] = 0;
        memcpy(*output, input, len);
        free(input);
    } 
    else 
    {
        *output = (char*)0;
    }
}

DLGAPI Win32OpenDialog(const char *filters, const char *defaultPath, char **outPath)
{
    char *res;
	int retval = NFD_OpenDialog(filters, defaultPath, &res) == NFD_OKAY;
	CopyToWin32Memory(res, outPath);
	return retval;
}

DLGAPI Win32SaveDialog(const char *filters, const char *defaultPath, char **outPath)
{
    char *res;
	int retval =  NFD_SaveDialog(filters, defaultPath, &res) == NFD_OKAY;
	CopyToWin32Memory(res, outPath);
	return retval;
}

DLGAPI Win32PickFolder(const char *defaultPath, char **outPath)
{
    char *res;
	int retval = NFD_PickFolder(defaultPath, &res) == NFD_OKAY;
	CopyToWin32Memory(res, outPath);
	return retval;
}
