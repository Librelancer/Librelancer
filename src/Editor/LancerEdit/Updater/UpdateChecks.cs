using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Ini;

namespace LancerEdit.Updater;

[SelfSection("Updates")]
public class UpdateChecks : IniFile
{
    public bool Enabled;
    private MainWindow win;
    private string baseFolder;

    private string thisRid;
    private long thisBuild;

    [Entry("channel")]
    public string UpdateChannel;
    [Entry("server")]
    public string UpdateUrl;

    public UpdateChecks(MainWindow window, string baseFolder)
    {
        this.win = window;
        this.baseFolder = baseFolder;
        try
        {
            LoadInfo();
        }
        catch (Exception e)
        {
            FLLog.Error("Updater", $"Couldn't load updater info. {e}");
            Enabled = false;
        }
        if(Enabled)
            FLLog.Info("Updater", "Updater enabled");
        else
            FLLog.Info("Updater", "Updater disabled");
    }

    class JsonUpdates
    {
        public record Build(string URL, long Timestamp);
        public Dictionary<string, Build> Builds { get; set; } = new Dictionary<string, Build>();
    }
    public UpdatePopup CheckForUpdates()
    {
        if (!Enabled)
            return null;
        var popup = new UpdatePopup(this);
        Task.Run(async () =>
        {
            using var http = new HttpClient();
            var json = await http.GetFromJsonAsync<JsonUpdates>(UpdateUrl + UpdateChannel + ".json", popup.Token);
            var b = json.Builds[thisRid];
            return b.Timestamp > thisBuild ? b.URL : null;
        }).ContinueWith(res =>
        {
            if (res.IsCompletedSuccessfully)
            {
                if(string.IsNullOrWhiteSpace(res.Result))
                    popup.Message($"{Icons.Check} You are up to date");
                else
                    popup.NewVersion(res.Result);
            }
            else
            {
                popup.Message($"{Icons.X} An error has occurred\n{res.Exception}");
            }
        });
        return popup;
    }

    public void Update(string executable)
    {
        win.QueueUIThread(() =>
        {
            win.RequestExit = true;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Extract updater exe from archive, as it can't overwrite itself
                using (var zipStream = File.OpenRead(executable)) {
                    using (var archive = new ZipArchive(zipStream))
                    {
                        var file = archive.Entries
                            .Select(e =>
                                new
                                {
                                    Name = string.Join("/", e.FullName.Replace('\\', '/').Split('/').Skip(1)),
                                    Entry = e
                                })
                            .FirstOrDefault(x => x.Name.Equals("lib/updater.exe", StringComparison.OrdinalIgnoreCase));
                        if (file != null)
                        {
                            using var updater = File.Create(Path.Combine(baseFolder, "lib/updater.exe"));
                            using var src = file.Entry.Open();
                            src.CopyTo(updater);
                        }
                    }
                }
                //Run updater
                Process.Start(Path.Combine(baseFolder, "lib/updater.exe"),
                    $"{Shell.Quote(executable)} {Shell.Quote(baseFolder)}");
            }
            else
            {
                Process.Start(executable, $"{Shell.Quote(baseFolder)} {Process.GetCurrentProcess().Id}");
            }
        });
    }

    void LoadInfo()
    {
        var path = Path.GetDirectoryName(typeof(UpdateChecks).Assembly.Location);
        if (path == null)
            return;
        //Check all the desired info exists
        if (!File.Exists(Path.Combine(path, "build.txt")) ||
            !File.Exists(Path.Combine(path, "updates.ini")) ||
            !File.Exists(Path.Combine(path, "manifest.txt"))) {
            return;
        }
        var x = File.ReadAllText(Path.Combine(path, "build.txt")).Split(';',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (x.Length != 2)
            return;
        thisRid = x[0];
        if (!long.TryParse(x[1], out thisBuild))
            return;
        ParseAndFill(Path.Combine(path, "updates.ini"), null);
        if (string.IsNullOrWhiteSpace(UpdateUrl))
            return;
        win.Config.UpdateChannel ??= UpdateChannel;
        UpdateChannel = win.Config.UpdateChannel;
        Enabled = !string.IsNullOrEmpty(UpdateChannel);
    }
}
