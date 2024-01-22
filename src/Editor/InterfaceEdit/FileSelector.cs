// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace InterfaceEdit
{
    public class FileSelector
    {
        private static int _unique = 9;
        private int unique = _unique++;
        private string baseDir;
        private string[] directoryNames;
        private string[] fileNames;
        private int fileNameSelected = -1;
        List<string> pathBar = new List<string>();
        public Func<string, bool> Filter = AnyFilter;
        public FileSelector(string baseDir)
        {
            this.baseDir = baseDir;
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
        public static bool AnyFilter(string x)
        {
            return true;
        }
        void Populate()
        {
            var path = baseDir;
            fileNameSelected = -1;
            foreach (var p in pathBar)
            {
                path = Path.Combine(path, p);
            }
            directoryNames = Directory.GetDirectories(path).Select(Path.GetFileName).OrderBy(x => x).ToArray();
            fileNames = Directory.GetFiles(path).Where(Filter).Select(Path.GetFileName).OrderBy(x => x).ToArray();
        }

        public void Open()
        {
            Populate();
            ImGui.OpenPopup(ImGuiExt.IDWithExtra("File Selector", unique));
        }

        string BuildFullPath(string file)
        {
            if (pathBar.Count < 1) return file;
            var path = pathBar[0];
            for (int i = 1; i < pathBar.Count; i++)
                path = Path.Combine(path, pathBar[i]);
            return Path.Combine(path, file);
        }
        public string Draw()
        {
            bool pOpen = true;
            ImGui.SetNextWindowSize(new Vector2(300,300), ImGuiCond.FirstUseEver);
            if (ImGui.BeginPopupModal(ImGuiExt.IDWithExtra("File Selector",unique), ref pOpen))
            {
                bool hasFilename = fileNameSelected >= 0 && fileNameSelected < fileNames.Length;
                string selected = hasFilename
                    ? fileNames[fileNameSelected]
                    : "None";
                ImGui.Text($"Selected: {selected}");
                ImGui.SameLine();
                if (ImGuiExt.Button("Open", hasFilename))
                {
                    ImGui.CloseCurrentPopup();
                    return BuildFullPath(fileNames[fileNameSelected]);
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.Separator();
                if (ImGui.Button("/"))
                {
                    pathBar = new List<string>();
                    Populate();
                }
                ImGui.SameLine();
                for (int i = 0; i < pathBar.Count; i++)
                {
                    if (ImGui.Button(ImGuiExt.IDWithExtra(pathBar[i], i)))
                    {
                        pathBar = pathBar.Take(i + 1).ToList();
                        Populate();
                    }

                    ImGui.SameLine();
                }
                ImGui.Dummy(new Vector2(1, 1));
                ImGui.BeginChild("##currentDir", new Vector2(-1, -1), ImGuiChildFlags.Border);
                for (int i = 0; i < directoryNames.Length; i++)
                {
                    IconSelectable(
                        Icons.Open,
                        ImGuiExt.IDWithExtra(directoryNames[i], i),
                        false,
                        out bool doubleClicked
                        );
                    if(doubleClicked)
                    {
                        pathBar.Add(directoryNames[i]);
                        Populate();
                    }
                }
                for (int i = 0; i < fileNames.Length; i++)
                {
                    if (IconSelectable(Icons.File,
                        ImGuiExt.IDWithExtra(fileNames[i], -i),
                        fileNameSelected == i, out bool doubleClicked))
                        fileNameSelected = i;
                    if (doubleClicked)
                    {
                        ImGui.CloseCurrentPopup();
                        return BuildFullPath(fileNames[fileNameSelected]);
                    }
                }
                ImGui.EndChild();
                ImGui.EndPopup();
            }
            return null;
        }

        static bool IconSelectable(char icon, string text,  bool selected, out bool doubleClicked)
        {
            var ret = ImGui.Selectable($"{icon}  {text}", selected);
            doubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);
            return ret;
        }
    }
}
