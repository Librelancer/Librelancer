using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using SimpleMesh;

namespace LibreLancer.ContentEdit;

public class Blender
{
    public static string AutodetectBlender()
    {
        if (Platform.RunningOS == OS.Windows)
        {
            //Try and get highest blender version from Program Files
            var blenderDefaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Blender Foundation");
            if (!Directory.Exists(blenderDefaultDir)) return null;
            var blenderDir = Directory.GetDirectories(blenderDefaultDir).Where(x => x.StartsWith("Blender ")).MaxBy(x => x);
            if (blenderDir == null) return null;
            if (File.Exists(Path.Combine(blenderDir, "blender.exe")))
                return Path.Combine(blenderDir, "blender.exe");
            return null;
        }
        if (Shell.HasCommand("blender")) return "blender";
        if (Shell.HasCommand("flatpak"))
        {
            //Load from flatpak
            var installed = Shell.GetString("flatpak", "list --columns=application");
            if (installed.Contains("org.blender.Blender"))
                return "FLATPAK";
        }
        return null;
    }

    public static bool FileIsBlender(string filename)
    {
        using var stream = File.OpenRead(filename);
        return FileIsBlender(stream);
    }

    public static bool FileIsBlender(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[9];
        if (stream.Read(bytes) != 9) return false;
        if (bytes[0] != 0x42 || //BLENDER magic
            bytes[1] != 0x4C ||
            bytes[2] != 0x45 ||
            bytes[3] != 0x4E ||
            bytes[4] != 0x44 ||
            bytes[5] != 0x45 ||
            bytes[6] != 0x52)
            return false;
        if (bytes[7] != 0x2D && bytes[7] != 0x5F) //- or _ pointer size
            return false;
        if (bytes[8] != 0x76 && bytes[7] != 0x56) //v or V endianness
            return false;
        return true;
    }

    public static bool BlenderPathValid(string blenderPath = null)
    {
        if (string.IsNullOrWhiteSpace(blenderPath))
            blenderPath = AutodetectBlender();
        if (string.IsNullOrWhiteSpace(blenderPath))
            return false;
        if (blenderPath == "FLATPAK")
            return Shell.GetString("flatpak", "run org.blender.Blender --version", 10000)
                .StartsWith("Blender");
        return Shell.GetString(blenderPath, "--version", 10000)
                .StartsWith("Blender");
    }

    static string EscapeCode(string s) => JsonValue.Create(s).ToJsonString();

    public static async Task<EditResult<SimpleMesh.Model>> LoadBlenderFile(string file, CancellationToken cancellation = default, Action<string> log = null, string blenderPath = null)
    {
        if (string.IsNullOrWhiteSpace(blenderPath))
            blenderPath = AutodetectBlender();
        if (string.IsNullOrWhiteSpace(blenderPath))
            return EditResult<SimpleMesh.Model>.Error("Could not locate blender executable");
        string name = "";
        string tmpfile = "";
        string tmppython = "";
        string tmpblend = "";
        string args = "";
        tmppython = Path.GetTempFileName();
        tmpfile = Path.GetTempFileName();
        File.Delete(tmpfile);
        if (blenderPath == "FLATPAK")
        {
            name = "flatpak";
            tmpblend = Path.GetTempFileName();
            File.Copy(file, tmpblend, true);
            args = $"run --filesystem=/tmp org.blender.Blender \"{tmpblend}\" --background --factory-startup --python \"{tmppython}\"";
        }
        else
        {
            name = blenderPath;
            args = $"\"{file}\" --background --factory-startup --python \"{tmppython}\"";
        }

        var exportCode =
            "import bpy\n"
            + $"bpy.ops.export_scene.gltf(filepath={EscapeCode(tmpfile)}, export_format='GLB', export_extras=True, use_mesh_edges=True)";
        File.WriteAllText(tmppython, exportCode);
        var psi = new ProcessStartInfo(name, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = log != null,
            RedirectStandardError = log != null
        };
        log?.Invoke($"Running {name} {args}\n");
        var p = Process.Start(psi);
        if (log != null)
        {
            p.OutputDataReceived += (o, e) =>
            {
                if (e.Data != null) log(e.Data + "\n");
            };
            p.ErrorDataReceived += (o, e) =>
            {
                if (e.Data != null) log(e.Data + "\n");
            };
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
        }

        try
        {
            await p.WaitForExitAsync(cancellation);
        }
        catch (TaskCanceledException)
        {
            p.CancelErrorRead();
            p.CancelOutputRead();
            p.TryKill();
            File.Delete(tmppython);
            if(!string.IsNullOrWhiteSpace(tmpblend)) File.Delete(tmpblend);
            return EditResult<SimpleMesh.Model>.Error("Blender import cancelled");
        }
        tmpfile += ".glb";
        File.Delete(tmppython);
        if(!string.IsNullOrWhiteSpace(tmpblend)) File.Delete(tmpblend);
        if(File.Exists(tmpfile))
        {
            return await EditResult<SimpleMesh.Model>.RunBackground(() =>
            {
                var bytes = File.ReadAllBytes(tmpfile);
                File.Delete(tmpfile);
                using var ms = new MemoryStream(bytes);
                return EditResult<SimpleMesh.Model>.TryCatch(() => SimpleMesh.Model.FromStream(ms));
            }, cancellation);
        }
        return EditResult<SimpleMesh.Model>.Error("Failed to execute blender export");
    }

    private const string EXPORT_SCRIPT = @"
import bpy

bpy.ops.wm.read_homefile(use_empty=True)
bpy.ops.import_scene.gltf(filepath={0})

for obj in bpy.context.scene.objects:
    obj.empty_display_size = 2
    obj.empty_display_type = 'SINGLE_ARROW'

bpy.ops.wm.save_as_mainfile(filepath={1})
";

    public static EditResult<bool> ExportBlenderFile(SimpleMesh.Model exported, string file, string blenderPath = null, Action<string> logLine = null)
    {
        if (string.IsNullOrWhiteSpace(blenderPath))
            blenderPath = AutodetectBlender();
        if (string.IsNullOrWhiteSpace(blenderPath))
            return EditResult<bool>.Error("Could not locate blender executable");
        string name = "";
        string tmpfile = "";
        string tmppython = "";
        string tmpblend = "";
        string args = "";
        tmppython = Path.GetTempFileName();
        tmpblend = Path.GetTempFileName();
        File.Delete(tmpblend);
        tmpfile = Path.GetTempFileName();
        using (var gltfStream = File.Create(tmpfile)) {
            exported.SaveTo(gltfStream, ModelSaveFormat.GLTF2);
        }
        if (blenderPath == "FLATPAK")
        {
            name = "flatpak";
            args = $"run --filesystem=/tmp org.blender.Blender --background --factory-startup --python \"{tmppython}\"";
        }
        else
        {
            name = blenderPath;
            args = $"\"{file}\" --background --factory-startup --python \"{tmppython}\"";
        }
        File.WriteAllText(tmppython, string.Format(EXPORT_SCRIPT, EscapeCode(tmpfile), EscapeCode(tmpblend)));
        var psi = new ProcessStartInfo(name, args);
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        using var p = Process.Start(psi);
        var builder = new StringBuilder();
        p.OutputDataReceived += (o, e) =>
        {
            if (e.Data != null) builder.AppendLine(e.Data);
        };
        p.ErrorDataReceived += (o, e) =>
        {
            if (e.Data != null) builder.AppendLine(e.Data);
        };
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
        p.WaitForExit();
        File.Delete(tmppython);
        File.Delete(tmpfile);
        if (File.Exists(tmpblend))
        {
            File.Move(tmpblend, file, true);
            return new EditResult<bool>(true);
        }

        return EditResult<bool>.Error($"Failed to execute blender import script\nExit Code: {p.ExitCode}\nLog: {builder.ToString()}");
    }
}
