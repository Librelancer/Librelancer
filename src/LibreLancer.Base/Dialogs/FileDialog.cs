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
	public class FileDialog
	{
        unsafe struct NFDString : IDisposable
        {
            public IntPtr Pointer;
            public static implicit operator void*(NFDString str) => (void*)str.Pointer;
            public void Dispose()
            {
                if(Pointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(Pointer);
            }
            public static NFDString Create(string str)
            {
                if (str == null) return new NFDString();
                if (Platform.RunningOS == OS.Windows)
                    return new NFDString() {Pointer = Marshal.StringToHGlobalUni(str)};
                else
                    return new NFDString() {Pointer = UnsafeHelpers.StringToHGlobalUTF8(str)};
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
                    var n = NFDString.Create(filters.Filters[i].Name);
                    var spec = NFDString.Create(string.Join(',', filters.Filters[i].Extensions));
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


        static unsafe void ThrowNFDError() => throw new Exception(Marshal.PtrToStringUTF8((IntPtr) NFD.NFD_GetError()));
        public static unsafe void Open(Action<string> onOpen, FileDialogFilters filters = null, string defaultPath = null)
        {
            NFD.NFD_ClearError();
            var f = NFDFilters.Create(filters);
            void* path = null;
            using var def = NFDString.Create(defaultPath);
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
