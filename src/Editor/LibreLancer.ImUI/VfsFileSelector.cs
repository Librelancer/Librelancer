// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.IO;

namespace LibreLancer.ImUI;

public class VfsFileSelector : PopupWindow
{
    private VfsFileSelectorControl control;

    private Action<string> onSelect;

    public override string Title { get; set; } = "Title";
    public override Vector2 InitSize => new (300);

    public VfsFileSelector(string title, FileSystem fs, string baseDir, Action<string> onSelect, Func<string,bool>? filter)
    {
        control = new("##control", fs, baseDir, filter);
        this.Title = title;
        this.onSelect = onSelect;
    }

    public static Func<string, bool> MakeFilter(params string[] extensions)
    {
        return (file) =>
        {
            var ext = Path.GetExtension(file);
            foreach (var test in extensions)
            {
                if (test.Equals(ext, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        };
    }

    public override void Draw(bool appearing)
    {
        switch (control.Draw(out var selectedFile))
        {
            case FileSelectorState.Selected:
                onSelect(selectedFile!);
                ImGui.CloseCurrentPopup();
                break;
            case FileSelectorState.Cancel:
                ImGui.CloseCurrentPopup();
                break;
        }
    }
}
