using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LancerEdit;

public static class Win32Integration
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private static void Register(string extension, string filetype, string icon)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        RegistryKey fileReg = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + extension);
        var name = $"librelancer-{extension.TrimStart('.')}";
        fileReg.SetValue("", name);
        RegistryKey appReg = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + name);
        appReg.SetValue("", filetype);
        var iconKey = appReg.CreateSubKey("DefaultIcon");
        iconKey.SetValue("", icon + ",0");
        var applicationPath = Process.GetCurrentProcess().MainModule.FileName;
        appReg.CreateSubKey("Shell\\Open\\Command").SetValue("", $"\"{applicationPath}\" \"%1\"");
    }
    public static void FileTypes()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        var iconsFolder = Path.Combine(Path.GetDirectoryName(typeof(Win32Integration).Assembly.Location) ?? string.Empty, "Icons");

        void Icon(string extension, string filetype, string icon)
        {
            var file = Path.Combine(iconsFolder, icon);
            if (File.Exists(file))
                Register(extension, filetype, file);
        }

        Icon(".3db", "3db model file","3DB.ico");
        Icon(".ale", "Alchemy effect file","ALE.ico");
        Icon(".anm", "Animation file","ANM.ico");
        Icon(".cmp", "Compound model file","CMP.ico");
        Icon(".mat", "Material file","MAT.ico");
        Icon(".sur", "Surface file","SUR.ico");
        Icon(".txm", "Texture library file","TXM.ico");
        Icon(".utf", "Utf file", "UTF.ico");
        Icon(".vms", "Visual mesh library file","VMS.ico");
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }
}
