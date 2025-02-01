// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibreLancer.Dialogs
{
    public class FileDialogFilters
    {
        public FileFilter[] Filters;
        public FileDialogFilters(params FileFilter[] filters)
        {
            Filters = filters;
        }

        public static FileDialogFilters operator +(FileDialogFilters left, FileDialogFilters right)
        {
            return new FileDialogFilters(left.Filters.Concat(right.Filters).ToArray());
        }
    }
    public class FileFilter
    {
        public string Name;
        public string[] Extensions;
        public FileFilter(string name, params string[] exts)
        {
            Name = name;
            Extensions = exts;
        }
    }
	public static class FileDialog
    {
        // SDL3 dialogs are reliant on the main loop
        // Use NFD if a script needs a dialog
        internal static IntPtr SDL3Handle = IntPtr.Zero;

        unsafe struct Utf8Native : IDisposable
        {
            public IntPtr Pointer;
            public static implicit operator void*(Utf8Native str) => (void*)str.Pointer;
            public static implicit operator byte*(Utf8Native str) => (byte*)str.Pointer;

            public void Dispose()
            {
                if(Pointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(Pointer);
            }
            public static Utf8Native Create(string str)
            {
                if (str == null) return new Utf8Native();
                if (Platform.RunningOS == OS.Windows)
                    return new Utf8Native() {Pointer = Marshal.StringToHGlobalUni(str)};
                else
                    return new Utf8Native() {Pointer = UnsafeHelpers.StringToHGlobalUTF8(str)};
            }
        }

        unsafe struct NFDFilters : IDisposable
        {
            public IntPtr Pointer;
            public uint Count;
            public List<IntPtr> ToFree;
            public static implicit operator NFD.NFDFilterItem*(NFDFilters flt) => (NFD.NFDFilterItem*)flt.Pointer;
            public static NFDFilters Create(FileDialogFilters filters)
            {
                var f = new NFDFilters();
                f.ToFree = new List<IntPtr>();
                if (filters?.Filters == null || filters.Filters.Length == 0)
                    return f;
                NFD.NFDFilterItem* items =
                    (NFD.NFDFilterItem*) Marshal.AllocHGlobal(sizeof(NFD.NFDFilterItem) * filters.Filters.Length);
                for (int i = 0; i < filters.Filters.Length; i++)
                {
                    var n = Utf8Native.Create(filters.Filters[i].Name);
                    var spec = Utf8Native.Create(string.Join(',', filters.Filters[i].Extensions));
                    items[i].name = n;
                    items[i].spec = spec;
                    f.ToFree.Add(n.Pointer);
                    f.ToFree.Add(spec.Pointer);
                }
                f.Count = (uint)filters.Filters.Length;
                f.Pointer = (IntPtr) items;
                f.ToFree.Add((IntPtr)items);
                return f;
            }

            public void Dispose()
            {
                foreach(var p in ToFree)
                    Marshal.FreeHGlobal(p);
            }
        }

        static unsafe string FromNFD(void* input)
        {
            if (input == null) return null;
            else if (Platform.RunningOS == OS.Windows)
                return Marshal.PtrToStringUni((IntPtr) input);
            else
                return Marshal.PtrToStringUTF8((IntPtr) input);
        }


        unsafe struct SDLFilters : IDisposable
        {
            public IntPtr Pointer;
            public uint Count;
            public List<IntPtr> ToFree;
            public static SDLFilters Create(FileDialogFilters filters)
            {
                var f = new SDLFilters();
                f.ToFree = new List<IntPtr>();
                var fitems = filters?.Filters ?? [];
                SDL3.SDL_DialogFileFilter* items =
                    (SDL3.SDL_DialogFileFilter*) Marshal.AllocHGlobal(sizeof(SDL3.SDL_DialogFileFilter) * (fitems.Length + 1));
                for (int i = 0; i < fitems.Length; i++)
                {
                    var n = Utf8Native.Create(fitems[i].Name);
                    var spec = Utf8Native.Create(string.Join(';', fitems[i].Extensions));
                    items[i].name = n;
                    items[i].pattern = spec;
                    f.ToFree.Add(n.Pointer);
                    f.ToFree.Add(spec.Pointer);
                }
                var afn = Utf8Native.Create("All Files");
                var afp = Utf8Native.Create("*");
                items[fitems.Length].name = afn;
                items[fitems.Length].pattern = afp;
                f.ToFree.Add(afn.Pointer);
                f.ToFree.Add(afp.Pointer);
                f.Count = (uint)(fitems.Length + 1);
                f.Pointer = (IntPtr) items;
                f.ToFree.Add((IntPtr)items);
                return f;
            }

            public void Dispose()
            {
                foreach(var p in ToFree)
                    Marshal.FreeHGlobal(p);
            }
        }


        static unsafe void ThrowNFDError() => throw new Exception(Marshal.PtrToStringUTF8((IntPtr) NFD.NFD_GetError()));
        public static unsafe void Open(Action<string> onOpen, FileDialogFilters filters = null, string defaultPath = null)
        {
            if (SDL3Handle != IntPtr.Zero)
            {
                using var sf = SDLFilters.Create(filters);
                void Callback(IntPtr userdata, IntPtr filelist, int filter)
                {
                    var self = GCHandle.FromIntPtr(userdata);
                    self.Free();
                    if (filelist == IntPtr.Zero)
                        return;
                    IntPtr* files = (IntPtr*)filelist;
                    if (*files == IntPtr.Zero)
                        return;
                    onOpen(Marshal.PtrToStringUTF8(*files));
                }
                SDL3.SDL_DialogFileCallback cb = Callback;
                var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
                SDL3.SDL_ShowOpenFileDialog(cb,
                    (IntPtr)cbh,
                    SDL3Handle,
                    new Span<SDL3.SDL_DialogFileFilter>((void*)sf.Pointer, (int)sf.Count),
                    (int)sf.Count,
                    defaultPath,
                    true);
                return;
            }
            NFD.NFD_ClearError();
            var f = NFDFilters.Create(filters);
            void* path = null;
            using var def = Utf8Native.Create(defaultPath);
            var res = NFD.NFD_OpenDialogN(&path, f, f.Count, (void*)def.Pointer);
            if (res == NFDResult.NFD_OKAY)
            {
                var selected = FromNFD(path);
                NFD.NFD_FreePathN(path);
                onOpen(selected);
            }
            else if (res == NFDResult.NFD_ERROR)
                ThrowNFDError();
        }

        public static unsafe void ChooseFolder(Action<string> onOpen)
        {
            if (SDL3Handle != IntPtr.Zero)
            {
                void Callback(IntPtr userdata, IntPtr filelist, int filter)
                {
                    var self = GCHandle.FromIntPtr(userdata);
                    self.Free();
                    if (filelist == IntPtr.Zero)
                        return;
                    IntPtr* files = (IntPtr*)filelist;
                    if (*files == IntPtr.Zero)
                        return;
                    onOpen(Marshal.PtrToStringUTF8(*files));
                }
                SDL3.SDL_DialogFileCallback cb = Callback;
                var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
                SDL3.SDL_ShowOpenFolderDialog(cb, (IntPtr)cbh, SDL3Handle, null, false);
                return;
            }
            NFD.NFD_ClearError();
            void* path = null;
            var res = NFD.NFD_PickFolderN(&path, null);
            if (res == NFDResult.NFD_OKAY)
            {
                var selected = FromNFD(path);
                NFD.NFD_FreePathN(path);
                onOpen(selected);
            }
            else if (res == NFDResult.NFD_ERROR)
                ThrowNFDError();
        }

        public static unsafe void Save(Action<string> onSave, FileDialogFilters filters = null)
		{
            if (SDL3Handle != IntPtr.Zero)
            {
                using var sf = SDLFilters.Create(filters);
                void Callback(IntPtr userdata, IntPtr filelist, int filter)
                {
                    var self = GCHandle.FromIntPtr(userdata);
                    self.Free();
                    if (filelist == IntPtr.Zero)
                        return;
                    IntPtr* files = (IntPtr*)filelist;
                    if (*files == IntPtr.Zero)
                        return;
                    onSave(Marshal.PtrToStringUTF8(*files));
                }
                SDL3.SDL_DialogFileCallback cb = Callback;
                var cbh = GCHandle.Alloc(cb, GCHandleType.Normal);
                SDL3.SDL_ShowSaveFileDialog(cb,
                    (IntPtr)cbh,
                    SDL3Handle,
                    new Span<SDL3.SDL_DialogFileFilter>((void*)sf.Pointer, (int)sf.Count),
                    (int)sf.Count,
                    null);
                return;
            }
            NFD.NFD_ClearError();
            var f = NFDFilters.Create(filters);
            void* path = null;
            var res = NFD.NFD_SaveDialogN(&path, f, f.Count, null, null);
            if (res == NFDResult.NFD_OKAY)
            {
                var selected = FromNFD(path);
                NFD.NFD_FreePathN(path);
                onSave(selected);
            }
            else if (res == NFDResult.NFD_ERROR)
                ThrowNFDError();
		}


	}
}
