using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Save;
using LibreLancer.Interface;
using MoonSharp.Interpreter;

namespace LibreLancer
{
    [MoonSharpUserData]
    public class SaveGameFolder : ITableData
    {
        class SaveGameFile
        {
            public string Path;
            public SaveGame Save;
        }

        private List<SaveGameFile> files = new List<SaveGameFile>();
        private InfocardManager infocards;

        [MoonSharpHidden] public string SelectedFile => ValidSelection() ? files[Selected].Path : null;
        public int Count => files.Count;
        public int Selected { get; set; }
        public string GetContentString(int row, string column)
        {
            if (row < 0 || row >= files.Count) return "";
            if (column == "name")
            {
                return files[row].Save.Player.Description ??
                       infocards.GetStringResource(files[row].Save.Player.DescripStrid);
            }
            else if (column == "date")
            {
                if (files[row].Save.Player.TimeStamp == null) return "";
                return
                    $"{files[row].Save.Player.TimeStamp.Value.ToShortDateString()} {files[row].Save.Player.TimeStamp.Value.ToShortTimeString()}";
            }
            return "[invalid]";
        }

        public SaveGameFolder(string folder, InfocardManager infocards)
        {
            FLLog.Info("Save", $"Loading folder {folder}");
            this.infocards = infocards;
            if (Directory.Exists(folder)) {
                files = LoadFiles(folder).OrderByDescending(x => x.Save.Player.TimeStamp.HasValue)
                    .ThenByDescending(x => x.Save.Player.TimeStamp).ToList();
                FLLog.Info("Save", $"Loaded {files.Count} saves");
            }
            else {
                files = new List<SaveGameFile>();
                FLLog.Info("Save", "Folder does not exist");
            }
        }

        static IEnumerable<SaveGameFile> LoadFiles(string folder)
        {
            foreach (var f in Directory.GetFiles(folder, "*.fl"))
            {
                SaveGame sg = null;
                try
                {
                    sg = SaveGame.FromFile(f);
                }
                catch (Exception)
                {
                    FLLog.Error("Save", $"Can't load save game `{f}`");
                    continue;
                }
                if (sg.Player == null) {
                    FLLog.Error("Save", $"Can't load save game `{f}`");
                }
                else if (!Path.GetFileNameWithoutExtension(f).Equals("restart", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new SaveGameFile() {Path = f, Save = sg};
                }
            }
        }

        public void TryDelete(int index)
        {
            if (!ValidSelection()) return;
            try
            {
                File.Delete(files[index].Path);
                files.RemoveAt(index);
                Selected = -1;
            }
            catch { }
        }

        public bool ValidSelection()
        {
            return Selected >= 0 && Selected < files.Count;
        }
    }
}