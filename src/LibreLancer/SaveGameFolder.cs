using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Interface;
using WattleScript.Interpreter;

namespace LibreLancer
{
    [WattleScriptUserData]
    public class SaveGameFolder : ITableData
    {
        private List<MetaSave> files = new List<MetaSave>();
        private InfocardManager infocards;

        [WattleScriptHidden] public InfocardManager Infocards
        {
            get => infocards;
            set => infocards = value;
        }
        [WattleScriptHidden] public string SelectedFile => ValidSelection() ? files[Selected].Filename : null;
        public int Count => files.Count;
        public int Selected { get; set; }
        public string GetContentString(int row, string column)
        {
            if (row < 0 || row >= files.Count) return "";
            if (column == "name")
            {
                return GetDescription(row);
            }
            else if (column == "date")
            {
                if (files[row].Timestamp == null) return "";
                return
                    $"{files[row].Timestamp.Value.ToShortDateString()} {files[row].Timestamp.Value:HH:mm}";
            }
            return "[invalid]";
        }

        string GetDescription(int row)
        {
            return files[row].Description ??
                   infocards.GetStringResource(files[row].DescriptionStrid);
        }

        private string folder;

        public SaveGameFolder() { }

        public void Load(string folder)
        {
            FLLog.Info("Save", $"Loading folder {folder}");
            this.infocards = infocards;
            this.folder = folder;
            if (Directory.Exists(folder)) {
                files = LoadFiles(folder).OrderByDescending(x => x.Timestamp.HasValue)
                    .ThenByDescending(x => x.Timestamp).ToList();
                FLLog.Info("Save", $"Located {files.Count} saves");
            }
            else {
                files = new List<MetaSave>();
                FLLog.Info("Save", "Folder does not exist");
            }
        }

        public void AddFile(string path)
        {
            Selected = -1;
            files.Add(MetaSave.FromFile(path));
            files = files.OrderByDescending(x => x.Timestamp.HasValue)
                .ThenByDescending(x => x.Timestamp).ToList();
        }

        static IEnumerable<MetaSave> LoadFiles(string folder)
        {
            foreach (var f in Directory.GetFiles(folder, "*.fl"))
            {
                MetaSave sg = null;
                try
                {
                    sg = MetaSave.FromFile(f);
                }
                catch (Exception e)
                {
                    FLLog.Error("Save", $"Can't load save game `{f}`");
                    continue;
                }
                if (!Path.GetFileNameWithoutExtension(f).Equals("restart", StringComparison.OrdinalIgnoreCase))
                {
                    yield return sg;
                }
            }
        }

        public void TryDelete(int index)
        {
            if (!ValidSelection()) return;
            try
            {
                File.Delete(files[index].Filename);
                files.RemoveAt(index);
                Selected = -1;
            }
            catch { }
        }

        public string CurrentDescription()
        {
            return ValidSelection() ? GetDescription(Selected) : "";
        }

        public bool ValidSelection()
        {
            return Selected >= 0 && Selected < files.Count;
        }
    }
}
