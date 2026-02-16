// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibreLancer.Dialogs;

public class FileDialogFilters(params FileFilter[] filters)
{
    public readonly FileFilter[] Filters = filters;
    public static FileDialogFilters operator +(FileDialogFilters left, FileDialogFilters right)
    {
        return new FileDialogFilters(left.Filters.Concat(right.Filters).ToArray());
    }
}

public class FileFilter(string? name, params string[] exts)
{
    public readonly string? Name = name;
    public readonly string[] Extensions = exts;
}

public static class FileDialog
{
    // SDL3 dialogs are reliant on the main loop
    // Use NFD if a script needs a dialog
    internal static IntPtr Sdl3Handle = IntPtr.Zero;

    private unsafe struct Utf8Native : IDisposable
    {
        public NativeBuffer Buffer;
        public static implicit operator void*(Utf8Native str) => (void*)str.Buffer;
        public static implicit operator byte*(Utf8Native str) => (byte*)str.Buffer;

        public void Dispose()
        {
            Buffer?.Dispose();
        }
        public static Utf8Native Create(string? str)
        {
            if (str == null)
            {
                return new Utf8Native();
            }

            return Platform.RunningOS == OS.Windows
                ? new Utf8Native() {Buffer = UnsafeHelpers.StringToNativeUTF16(str)}
                : new Utf8Native() {Buffer = UnsafeHelpers.StringToNativeUTF8(str)};
        }
    }

    private unsafe struct NfdFilters : IDisposable
    {
        public IntPtr Pointer;
        public uint Count;
        public List<NativeBuffer> ToFree;
        public static implicit operator NFD.NFDFilterItem*(NfdFilters flt) => (NFD.NFDFilterItem*)flt.Pointer;

        public static NfdFilters Create(FileDialogFilters? filters)
        {
            var f = new NfdFilters();
            f.ToFree = [];
            if (filters?.Filters == null || filters.Filters.Length == 0)
            {
                return f;
            }

            var itemsBuffer = UnsafeHelpers.Allocate(sizeof(NFD.NFDFilterItem) * filters.Filters.Length);
            NFD.NFDFilterItem* items = (NFD.NFDFilterItem*)(IntPtr)itemsBuffer;

            for (var i = 0; i < filters.Filters.Length; i++)
            {
                var n = Utf8Native.Create(filters.Filters[i].Name);
                var spec = Utf8Native.Create(string.Join(',', filters.Filters[i].Extensions));
                items[i].name = n;
                items[i].spec = spec;
                f.ToFree.Add(n.Buffer);
                f.ToFree.Add(spec.Buffer);
            }

            f.Count = (uint)filters.Filters.Length;
            f.Pointer = (IntPtr) items;
            f.ToFree.Add(itemsBuffer);
            return f;
        }

        public void Dispose()
        {
            foreach (var p in ToFree)
            {
                p.Dispose();
            }
        }
    }

    private static unsafe string? FromNfd(void* input)
    {
        if (input == null)
        {
            return null;
        }
        else if (Platform.RunningOS == OS.Windows)
        {
            return Marshal.PtrToStringUni((IntPtr) input);
        }
        else
        {
            return Marshal.PtrToStringUTF8((IntPtr) input);
        }
    }

    private unsafe struct SDLFilters : IDisposable
    {
        public IntPtr Pointer;
        public uint Count;
        public List<NativeBuffer> ToFree;
        public static SDLFilters Create(FileDialogFilters? filters)
        {
            var f = new SDLFilters();
            f.ToFree = new List<NativeBuffer>();
            var fitems = filters?.Filters ?? [];
            var itemsBuffer = UnsafeHelpers.Allocate(sizeof(SDL3.SDL_DialogFileFilter) * (fitems.Length + 1));
            SDL3.SDL_DialogFileFilter* items =
                (SDL3.SDL_DialogFileFilter*)(IntPtr)itemsBuffer;
            for (var i = 0; i < fitems.Length; i++)
            {
                var n = Utf8Native.Create(fitems[i].Name);
                var spec = Utf8Native.Create(string.Join(';', fitems[i].Extensions));
                items[i].name = n;
                items[i].pattern = spec;
                f.ToFree.Add(n.Buffer);
                f.ToFree.Add(spec.Buffer);
            }

            var afn = Utf8Native.Create("All Files");
            var afp = Utf8Native.Create("*");
            items[fitems.Length].name = afn;
            items[fitems.Length].pattern = afp;
            f.ToFree.Add(afn.Buffer);
            f.ToFree.Add(afp.Buffer);
            f.Count = (uint)(fitems.Length + 1);
            f.Pointer = (IntPtr) items;
            f.ToFree.Add(itemsBuffer);
            return f;
        }

        public void Dispose()
        {
            foreach (var p in ToFree)
                p.Dispose();
        }
    }

    private static unsafe void ThrowNfdError() => throw new Exception(Marshal.PtrToStringUTF8((IntPtr) NFD.NFD_GetError()));
    public static unsafe void Open(Action<string?> onOpen, FileDialogFilters? filters = null, string? defaultPath = null)
    {
        if (Sdl3Handle != IntPtr.Zero)
        {
            using var sf = SDLFilters.Create(filters);
            SDL3.SDL_DialogFileCallback cb = Callback;
            var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
            SDL3.SDL_ShowOpenFileDialog(cb,
                (IntPtr)cbh,
                Sdl3Handle,
                new Span<SDL3.SDL_DialogFileFilter>((void*)sf.Pointer, (int)sf.Count),
                (int)sf.Count,
                defaultPath,
                true);

            return;

            void Callback(IntPtr userdata, IntPtr fileList, int filter)
            {
                var self = GCHandle.FromIntPtr(userdata);
                self.Free();

                if (fileList == IntPtr.Zero)
                {
                    return;
                }

                var files = (IntPtr*)fileList;
                if (*files == IntPtr.Zero)
                {
                    return;
                }

                onOpen(Marshal.PtrToStringUTF8(*files)!);
            }
        }

        NFD.NFD_ClearError();
        var f = NfdFilters.Create(filters);
        void* path = null;
        using var def = Utf8Native.Create(defaultPath);
        var res = NFD.NFD_OpenDialogN(&path, f, f.Count, (void*)def.Buffer);
        if (res == NFDResult.NFD_OKAY)
        {
            var selected = FromNfd(path);
            NFD.NFD_FreePathN(path);
            onOpen(selected);
        }
        else if (res == NFDResult.NFD_ERROR)
        {
            ThrowNfdError();
        }
    }

    public static unsafe void ChooseFolder(Action<string?> onOpen)
    {
        if (Sdl3Handle != IntPtr.Zero)
        {
            SDL3.SDL_DialogFileCallback cb = Callback;
            var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
            SDL3.SDL_ShowOpenFolderDialog(cb, (IntPtr)cbh, Sdl3Handle, null, false);
            return;

            void Callback(IntPtr userdata, IntPtr filelist, int filter)
            {
                var self = GCHandle.FromIntPtr(userdata);
                self.Free();
                if (filelist == IntPtr.Zero)
                {
                    return;
                }

                var files = (IntPtr*)filelist;
                if (*files == IntPtr.Zero)
                {
                    return;
                }

                onOpen(Marshal.PtrToStringUTF8(*files)!);
            }
        }

        NFD.NFD_ClearError();
        void* path = null;
        var res = NFD.NFD_PickFolderN(&path, null);

        if (res == NFDResult.NFD_OKAY)
        {
            var selected = FromNfd(path);
            NFD.NFD_FreePathN(path);
            onOpen(selected);
        }
        else if (res == NFDResult.NFD_ERROR)
        {
            ThrowNfdError();
        }
    }

    public static unsafe void Save(Action<string?> onSave, FileDialogFilters? filters = null)
    {
        if (Sdl3Handle != IntPtr.Zero)
        {
            using var sf = SDLFilters.Create(filters);
            SDL3.SDL_DialogFileCallback cb = Callback;
            var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
            SDL3.SDL_ShowSaveFileDialog(cb,
                (IntPtr)cbh,
                Sdl3Handle,
                new Span<SDL3.SDL_DialogFileFilter>((void*)sf.Pointer, (int)sf.Count),
                (int)sf.Count,
                null);
            return;

            void Callback(IntPtr userdata, IntPtr filelist, int filter)
            {
                var self = GCHandle.FromIntPtr(userdata);
                self.Free();
                if (filelist == IntPtr.Zero)
                {
                    return;
                }

                var files = (IntPtr*)filelist;
                if (*files == IntPtr.Zero)
                {
                    return;
                }

                onSave(Marshal.PtrToStringUTF8(*files)!);
            }
        }

        NFD.NFD_ClearError();
        var f = NfdFilters.Create(filters);
        void* path = null;
        var res = NFD.NFD_SaveDialogN(&path, f, f.Count, null, null);

        if (res == NFDResult.NFD_OKAY)
        {
            var selected = FromNfd(path);
            NFD.NFD_FreePathN(path);
            onSave(selected);
        }
        else if (res == NFDResult.NFD_ERROR)
        {
            ThrowNfdError();
        }
    }

    public static unsafe void OpenMultiple(
        Action<string[]> onOpen,
        FileDialogFilters? filters = null,
        string? defaultPath = null)
    {
        if (Sdl3Handle != IntPtr.Zero)
        {
            using var sf = SDLFilters.Create(filters);
            SDL3.SDL_DialogFileCallback cb = Callback;
            var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
            SDL3.SDL_ShowOpenFileDialog(cb,
                (IntPtr)cbh,
                Sdl3Handle,
                new Span<SDL3.SDL_DialogFileFilter>((void*)sf.Pointer, (int)sf.Count),
                (int)sf.Count,
                defaultPath,
                true);

            return;

            void Callback(IntPtr userdata, IntPtr filelist, int filter)
            {
                var self = GCHandle.FromIntPtr(userdata);
                self.Free();
                if (filelist == IntPtr.Zero)
                {
                    return;
                }

                var files = (IntPtr*)filelist;
                List<string> strings = [];
                var i = 0;
                while (files[i] != IntPtr.Zero)
                {
                    strings.Add(Marshal.PtrToStringUTF8(files[i])!);
                    i++;
                }
                onOpen(strings.ToArray());
            }
        }

        // fallback to useing Native File Dialogue - Extended
        NFD.NFD_ClearError();
        using var f = NfdFilters.Create(filters);

        void* pathSet = null;       // <-- this will receive nfdpathset_t*
        using var def = Utf8Native.Create(defaultPath);

        var res = NFD.NFD_OpenDialogMultipleN(
            &pathSet,
            f,
            f.Count,
            (void*)def.Buffer);

        if (res == NFDResult.NFD_CANCEL || pathSet == null)
        {
            onOpen([]);
            return;
        }

        if (res == NFDResult.NFD_ERROR)
        {
            ThrowNfdError();
            return;
        }

        // Get number of paths
        ulong count = 0;
        var countRes = NFD.NFD_PathSet_GetCount(pathSet, &count);
        if (countRes != NFDResult.NFD_OKAY)
        {
            NFD.NFD_PathSet_Free(pathSet);
            ThrowNfdError();
            return;
        }

        var results = new string[count];

        for (ulong i = 0; i < count; i++)
        {
            void* pathPtr = null;

            var getRes = NFD.NFD_PathSet_GetPathN(pathSet, i, &pathPtr);
            if (getRes == NFDResult.NFD_OKAY && pathPtr != null)
            {
                var str = FromNfd(pathPtr);
                if (str is not null)
                {
                    // Convert wchar_t* to UTF-16 string
                    results[i] = str;

                    // Free individual path (Windows: PathSet_FreePathN)
                    NFD.NFD_PathSet_FreePathN(pathPtr);
                }
            }
            else
            {
                results[i] = string.Empty;
            }
        }

        // Free entire path set
        NFD.NFD_PathSet_Free(pathSet);

        // Return the result
        onOpen(results);
    }
}
