using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

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
            return Shell.GetString("flatpak", "run org.blender.Blender --version")
                .StartsWith("Blender");
        return Shell.GetString(blenderPath, "--version")
                .StartsWith("Blender");
    }

    static string EscapeCode(string s) => JsonValue.Create(s).ToJsonString();
    
    public static EditResult<SimpleMesh.Model> LoadBlenderFile(string file, string blenderPath = null)
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
            args = $"run --filesystem=/tmp org.blender.Blender \"{tmpblend}\" --background --python \"{tmppython}\"";
        }
        else
        {
            name = blenderPath;
            args = $"\"{{file}}\" --background --python \"{tmppython}\"";
        }

        var exportCode =
            @$"import bpy
            bpy.ops.export_scene.gltf(filepath={EscapeCode(tmpfile)}, export_format='GLTF_EMBEDDED', export_extras=True)";
        File.WriteAllText(tmppython, exportCode);
        var p = Process.Start(name, args);
        p.WaitForExit();
        tmpfile += ".gltf";
        File.Delete(tmppython);
        if(!string.IsNullOrWhiteSpace(tmpblend)) File.Delete(tmpblend);
        if(File.Exists(tmpfile))
        {
            var bytes = File.ReadAllBytes(tmpfile);
            File.Delete(tmpfile);
            using var ms = new MemoryStream(bytes);
            return EditResult<SimpleMesh.Model>.TryCatch(() => SimpleMesh.Model.FromStream(ms));
        }
        return EditResult<SimpleMesh.Model>.Error("Failed to execute blender export");
    }
}