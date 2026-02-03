using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Dialogs;

internal enum NFDResult : int
{
    NFD_ERROR,
    NFD_OKAY,
    NFD_CANCEL
}

internal static unsafe class NFD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NFDFilterItem
    {
        public void* name;
        public void* spec;
    }

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_Init();

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern void NFD_Quit();


    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_OpenDialogN(
        void** outPath,
        NFDFilterItem* filterList,
        uint filterCount,
        void* defaultPath);

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_OpenDialogMultipleN(
        void** outPathSet,                   // const nfdpathset_t** in C
        NFDFilterItem* filterList,
        uint filterCount,
        void* defaultPath);

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_SaveDialogN(
        void** outPath,
        NFDFilterItem* filterList,
        uint filterCount,
        void* defaultPath,
        void* defaultName);

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_PickFolderN(
        void** outPath,
        void* defaultPath);

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern void NFD_ClearError();

    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern void* NFD_GetError();

    [DllImport("lancerdialogs", CallingConvention =  CallingConvention.Cdecl)]
    public static extern void NFD_PathSet_FreePathN(void* path);

    [DllImport("lancerdialogs", CallingConvention =  CallingConvention.Cdecl)]
    public static extern void NFD_FreePathN(void* path);

    // Count entries in the set
    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_PathSet_GetCount(
        void* pathSet,
        ulong* count);

    // Get path at index (returns new wchar_t* that MUST be freed)
    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern NFDResult NFD_PathSet_GetPathN(
        void* pathSet,
        ulong index,
        void** outPath);

    // Free entire pathset structure (NOT the individual paths)
    [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
    public static extern void NFD_PathSet_Free(
        void* pathSet);
}
