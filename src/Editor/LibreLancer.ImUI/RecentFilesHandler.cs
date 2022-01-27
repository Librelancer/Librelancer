// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ImGuiNET;

namespace LibreLancer.ImUI
{
    public class RecentFilesXml
    {
        public List<string> Files = new List<string>();
    }
    public class RecentFilesHandler
    {
        private RecentFilesXml data;
        private Action<string> open;
        public RecentFilesHandler(Action<string> openFile)
        {
            open = openFile;
            try
            {
                var path = CachePath();
                if (File.Exists(path)) {
                    data = new RecentFilesXml()
                    {
                        Files = File.ReadAllLines(path).Where((x) => !string.IsNullOrWhiteSpace(x))
                            .ToList()
                    };
                }
                else
                {
                    data = new RecentFilesXml();
                }
            }
            catch (Exception e)
            {
                data = new RecentFilesXml();
                FLLog.Error("Recent Files", e.Message);
            }
        }

        private bool openError = false;
        private string errorText;
        public void Menu()
        {
            if (data.Files.Count <= 0)
            {
                Theme.IconMenuItem(Icons.Open, "Open Recent", false);
            }
            else
            {
                if (Theme.BeginIconMenu(Icons.Open, "Open Recent"))
                {
                    int i = 0;
                    string toOpen = null;
                    string toDelete = null;
                    foreach (var item in ((IEnumerable<string>)data.Files).Reverse())
                    {
                        var fn = Path.GetFileName(item);
                        var dir = Path.GetDirectoryName(item);
                        var builder = new StringBuilder(32);
                        //Build shortened directory name
                        while (builder.Length < 32)
                        {
                            var n2 = Path.GetDirectoryName(dir);
                            if (n2 == null) break;
                            if (n2 == "/" || n2.EndsWith(":"))
                            {
                                string s = dir;
                                if (!Path.EndsInDirectorySeparator(dir)) s += Path.DirectorySeparatorChar;
                                builder.Insert(0, s);
                                break;
                            }
                            builder.Insert(0, $"{dir.Substring(n2.Length + 1)}{Path.DirectorySeparatorChar}");
                            dir = n2;
                        }
                        dir = builder.ToString();
                        //Do things
                        if (ImGui.MenuItem(ImGuiExt.IDWithExtra($"{fn} ({dir})", i++)))
                        {
                            if (!File.Exists(item))
                            {
                                openError = true;
                                errorText = $"File {item} was not found";
                                toDelete = item;
                            }
                            else toOpen = item;
                        }
                    }
                    if (toOpen != null)
                        open(toOpen);
                    if (toDelete != null)
                    {
                        data.Files.Remove(toDelete);
                        Save();
                    }
                    ImGui.EndMenu();
                }
            }
        }

        void Save()
        {
            try
            {
                File.WriteAllLines(CachePath(), data.Files);
            }
            catch (Exception)
            {
            }
        }
        public void DrawErrors()
        {
            if (openError)
            {
                ImGui.OpenPopup("Error##RecentFiles");
                openError = false;
            }

            bool pOpen = true;
            if (ImGui.BeginPopupModal("Error##RecentFiles", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Error:");
                ImGui.Text(errorText);
                if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }

        public void FileOpened(string path)
        {
            for(int i = 0; i < data.Files.Count; i++)
            {
                if (data.Files[i] == path)
                {
                    data.Files.RemoveAt(i);
                    break;
                }
            }
            data.Files.Add(path);
            if(data.Files.Count > 5)
                data.Files.RemoveAt(0);
            Save();
        }

        static string CachePath()
        {
            string directory;
            if (Platform.RunningOS == OS.Linux)
            {
                if (string.IsNullOrWhiteSpace((directory = Environment.GetEnvironmentVariable("XDG_CACHE_HOME"))))
                {
                    directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cache");
                }
            }
            else
            {
                directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            return Path.Combine(directory, "librelancer." + Assembly.GetEntryAssembly().GetName().Name + ".recentfiles.txt");
        }
    }
}