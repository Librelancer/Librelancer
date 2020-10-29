// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Interface;

namespace InterfaceEdit
{
    public static class Compiler
    {
        static bool CompileFilter(string x)
        {
            x = Path.GetFileName(x);
            if(!x.EndsWith(".xml", true, CultureInfo.InvariantCulture)) return false;
            if (x.Equals("stylesheet.xml", StringComparison.OrdinalIgnoreCase)) return false;
            if (x.Equals("resources.xml", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        static IEnumerable<string> CompileFiles(string folder) =>  Directory.GetFiles(folder)
            .Where(CompileFilter).OrderBy(x => x);

        static IEnumerable<string> LuaFiles(string folder) => Directory.GetFiles(folder).Where(
            x => x.EndsWith(".lua", true, CultureInfo.InvariantCulture)).Select(x => Path.GetFileName(x));

        private const string BOILERPLATE = @"
local active = {}
function OpenScene(s)
    local w = _classes[s]()
    SetActive(w)
end
function SetActive(w)
    active = w
    SetWidget(w.Widget)
    CallEvent('enter')
end
function CallEvent(ev, ...)
    if active[ev] ~= nil then
        active[ev](...)
    end
end
";
        public static void Compile(string xmlFolder, UiXmlLoader xmlLoader, string outfolder = null)
        {
            outfolder ??= Path.Combine(xmlFolder, "out");
            Directory.CreateDirectory(outfolder);
            var bundle = new InterfaceTextBundle();
            bundle.AddStringCompressed("resources.xml", File.ReadAllText(Path.Combine(xmlFolder, "resources.xml")));
            foreach (var file in LuaFiles(xmlFolder))
            {
                bundle.AddStringCompressed(file, File.ReadAllText(Path.Combine(xmlFolder, file)));
            }
            var mainlua = new StringBuilder();
            if (File.Exists(Path.Combine(xmlFolder, "main.lua")))
            {
                mainlua.AppendLine("require 'main.lua'");
            }
            mainlua.AppendLine("local _classes = {}");
            foreach (var file in CompileFiles(xmlFolder))
            {
                try
                {
                    var (classname, source) = xmlLoader.LuaClassDesigner(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
                    bundle.AddStringCompressed(classname + ".designer.lua", source);
                    File.WriteAllText(Path.Combine(outfolder, classname + ".designer.lua"), source);
                    mainlua.AppendLine($"require '{classname}.designer'");
                    if (File.Exists(Path.Combine(xmlFolder, classname + ".lua")))
                    {
                        mainlua.AppendLine($"require '{classname}'");
                    }
                    mainlua.AppendLine($"_classes.{classname} = {classname}");
                }
                catch (Exception e)
                {
                    throw new Exception($"Error compiling {file}", e);
                }
               
            }
            // uimain boilerplate
            mainlua.AppendLine(BOILERPLATE);
            //
            mainlua.AppendLine(xmlLoader.StylesheetToLua(File.ReadAllText(Path.Combine(xmlFolder, "stylesheet.xml"))));
            bundle.AddStringCompressed("uimain.lua", mainlua.ToString());
            File.WriteAllText(Path.Combine(outfolder, "interface.json"), bundle.ToJSON());
        }
    }
}