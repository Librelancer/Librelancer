using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// Why all this extra metadata? Reduces AV false positives
[assembly:AssemblyVersion("2024.10.0")]
[assembly:AssemblyFileVersion("2024.10.0")]
[assembly:AssemblyCulture("en")]
[assembly:AssemblyProduct("Librelancer Updater")]
[assembly:AssemblyCopyright("ⓒ Callum McGing 2024")]
[assembly:AssemblyCompany("Librelancer")]
[assembly:AssemblyDescription("Updater for Librelancer SDK builds")]

// The updater class itself

namespace LibreLancer
{


public class UpdateApplication : Form
{
    Label text;
    string dir;
    Stream zipStream;
    public UpdateApplication(string dir, Stream zipStream)
    {
        Size = new Size(250,150);
        Text = "Updating";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        Shown += OnShown;
        text = new Label();
        text.Text = "Updating SDK...";
        text.Dock = DockStyle.Fill;
        text.AutoSize = false;
        text.TextAlign = ContentAlignment.MiddleCenter;
        text.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        Controls.Add(text);
        this.zipStream = zipStream;
        this.dir = dir;
    }

    void Finished()
    {
        MessageBox.Show("Update complete", "Librelancer Update");
        Application.Exit();
    }

    private void OnShown(Object sender, EventArgs e)
    {
        Task.Run(() => {
            Run(dir, zipStream);
        }).ContinueWith(res => {
            Invoke((Action)Finished);
        });
    }

    static string[] Read(string filename, StreamWriter log)
    {
        try
        {
            return File.ReadAllLines(filename);
        }
        catch(Exception)
        {
            log.WriteLine("Can't open file " + filename);
            return new string[0];
        }
    }

    static void Delete(string filename, StreamWriter log)
    {
        try
        {
            File.Delete(filename);
            log.WriteLine("Deleted " + filename);
        }
        catch(Exception)
        {
            log.WriteLine("Can't delete file " + filename);
        }
    }

    static void Run(string outputDir, Stream baseStream)
    {
        Directory.CreateDirectory(outputDir);
        using(var writer = new StreamWriter(Path.Combine(outputDir, "LatestUpdateInfo.txt")))
        {
            Update(outputDir, baseStream, writer);
        }
    }

    static void Update(string outputDir, Stream baseStream, StreamWriter log)
    {
        var srcManifest = Read(Path.Combine(outputDir, "lib/manifest.txt"), log);
        //Unzip,
        using(var archive = new ZipArchive(baseStream))
        {
            foreach(var e in archive.Entries)
            {
                var n = string.Join("/", e.FullName.Replace('\\', '/').Split('/').Skip(1).ToArray());
                if(string.IsNullOrEmpty(n))
                    continue;
                Console.WriteLine(n);
                // We can't include the string updater.exe or it makes AV upset
                if(n.EndsWith(Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName), StringComparison.OrdinalIgnoreCase))
                    continue;
                if(n.EndsWith("/"))
                    Directory.CreateDirectory(Path.Combine(outputDir, n));
                else
                {
                    using(var dest = File.Create(Path.Combine(outputDir, n)))
                    {
                        using(var src = e.Open())
                        {
                            src.CopyTo(dest);
                        }
                    }
                }
            }
        }
        var dstManifest = Read(Path.Combine(outputDir, "lib/manifest.txt"), log);
        HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var a in dstManifest)
            files.Add(a);
        List<string> toDelete = new List<string>();
        foreach(var b in srcManifest)
            if(!files.Contains(b))
                toDelete.Add(b);
        foreach(var file in toDelete)
        {
            Delete(Path.Combine(outputDir, file), log);
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        if(args.Length < 2)
        {
            return;
        }
        Thread.Sleep(3000);
        Application.EnableVisualStyles();
        using(var stream = File.OpenRead(args[0]))
        {
            Application.Run(new UpdateApplication(args[1], stream));
        }
        File.Delete(args[0]);
    }
}

}
