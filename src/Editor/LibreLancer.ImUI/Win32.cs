// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibreLancer.ImUI
{
    static class Win32
    {
        public static string ConvertFilters(FileDialogFilters filters)
        {
            if (filters == null) return null;
            var builder = new StringBuilder();
            bool first = true;
            foreach(var f in filters.Filters)
            {
                if (!first)
                    builder.Append(";");
                first = false;
                builder.Append(string.Join(",", f.Extensions.Select((x) =>
                {
                    int index = x.IndexOf('.');
                    if (index != -1) return x.Substring(index + 1);
                    return x;
                })));
            }
            return builder.ToString();
        }
        [DllImport("win32dialogs.dll")]
        public static extern bool Win32OpenDialog(
            [MarshalAs(UnmanagedType.LPUTF8Str)]string filters,
            [MarshalAs(UnmanagedType.LPUTF8Str)]string defaultPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)]out string outPath
        );
        [DllImport("win32dialogs.dll")]
        public static extern bool Win32SaveDialog(
            [MarshalAs(UnmanagedType.LPUTF8Str)]string filters,
            [MarshalAs(UnmanagedType.LPUTF8Str)]string defaultPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)]out string outPath
        );

        [DllImport("win32dialogs.dll")]
        public static extern bool Win32PickFolder(
            [MarshalAs(UnmanagedType.LPUTF8Str)]string defaultPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)]out string outPath
        );
    }
}
