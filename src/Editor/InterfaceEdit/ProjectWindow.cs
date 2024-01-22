// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ImGuiNET;
using System.Numerics;
using LibreLancer.ImUI;

namespace InterfaceEdit
{
    public class ProjectWindow : IDisposable
    {
        private string folder;
        private string[] files;
        private MainWindow window;
        private FileSystemWatcher watcher;

        public ProjectWindow(string folder, MainWindow window)
        {
            this.folder = folder;
            this.window = window;
            LoadFiles();
            watcher = new FileSystemWatcher();
            watcher.Path = folder;

            // Should cover everything
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName;

             // Only watch xml files.
             watcher.Changed += FileSystemReload;
             watcher.Created += FileSystemReload;
             watcher.Deleted += FileSystemReload;
             watcher.Renamed += WatcherOnRenamed;
             watcher.EnableRaisingEvents = true;
        }


        public void Dispose()
        {
            watcher.Dispose();
        }

        bool FileFilter(string x)
        {
            if(!x.EndsWith(".xml", true, CultureInfo.InvariantCulture) &&
               !x.EndsWith(".lua", true, CultureInfo.InvariantCulture)) return false;
            if (x.Equals("stylesheet.xml", StringComparison.OrdinalIgnoreCase)) return false;
            if (x.Equals("resources.xml", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        void LoadFiles() => files = Directory.GetFiles(folder).Select(x => Path.GetFileName(x))
            .Where(FileFilter).OrderBy(x => x).ToArray();

        private void FileSystemReload(object sender, FileSystemEventArgs e) => LoadFiles();
        private void WatcherOnRenamed(object sender, RenamedEventArgs e) => LoadFiles();

        public bool IsOpen = false;

        private byte[] newFileBuffer = new byte[48];

        public IEnumerable<string> GetClasses()
        {
            return files.Where(x => x.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        }
        public unsafe void Draw()
        {
            bool openNew = false;
            if (IsOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(300,350), ImGuiCond.FirstUseEver);
                ImGui.Begin("Project", ref IsOpen);
                if (ImGui.Button("New"))
                {
                    openNew = true;
                    newFileBuffer = new byte[48];
                }
                ImGui.BeginChild("##files", Vector2.Zero, ImGuiChildFlags.Border);
                for (int i = 0; i < files.Length; i++)
                {
                    ImGui.Selectable(ImGuiExt.IDWithExtra(files[i], i));
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                    {
                        if (files[i].EndsWith(".lua", true, CultureInfo.InvariantCulture))
                            window.OpenLua(Path.Combine(folder, files[i]));
                        else
                            window.OpenXml(Path.Combine(folder, files[i]));
                    }
                }
                ImGui.EndChild();
                ImGui.End();
            }
            if(openNew) ImGui.OpenPopup("New File");
            if (ImGui.BeginPopupModal("New File"))
            {
                ImGui.InputText("##filename", newFileBuffer, 48, ImGuiInputTextFlags.CallbackCharFilter, Callback);
                if (ImGui.Button("Ok"))
                {
                    int length = 0;
                    for (length = 0; length < 48; length++)
                    {
                        if (newFileBuffer[length] == 0) break;
                    }
                    if(length == 0)
                        ImGui.CloseCurrentPopup();
                    else
                    {
                        var str = Encoding.UTF8.GetString(newFileBuffer, 0, length);
                        str = str.Trim();
                        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                        {
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            bool create = true;
                            foreach (var f in files)
                            {
                                if (f.Equals(str, StringComparison.OrdinalIgnoreCase))
                                {
                                    create = false;
                                    break;
                                }
                            }
                            if(create)
                                File.WriteAllText(Path.Combine(folder, str), "");
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        //hardcoded list of forbidden filename chars for xplatform
        static readonly char[] forbidden = { '/', '<', '>', ':', '"', '\\', '|', '?', '*' };
        private unsafe int Callback(ImGuiInputTextCallbackData* data)
        {
            for (int i = 0; i < forbidden.Length; i++)
            {
                if (data->EventChar == (ushort) forbidden[i]) return 1;
            }
            return 0;
        }
    }
}
