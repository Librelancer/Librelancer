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
local _classes = {}
function OpenScene(s)
    local w = _classes[s]()
    SetActive(w)
end
function SetActive(w)
    active = w
    SetWidget(w.Widget)
    CallEvent('enter')
end

function OpenModal(m)
   if m._modalinfo ~= nil then
        m._modalinfo.closehandle = Funcs:OpenModal(m.Widget)
    else
        error('Class is not modal type')
    end 
end

function SwapModal(m, m2)
    if m._modalinfo == nil or m2._modalinfo == nil then
        error('Class is not modal type')
    else
        m2._modalinfo.closehandle = m._modalinfo.closehandle
        Funcs:SwapModal(m2.Widget, m._modalinfo.closehandle)
    end
end

local function m_modalinit(self)
    self._modalinfo = {}
end
local function m_modalcallback(self, f)
    self._modalinfo.Callback = f
end
local function m_close(self, ...)
    Funcs:CloseModal(self._modalinfo.closehandle)
    if self._modalinfo.Callback ~= nil then
        self._modalinfo.Callback(...)
    end
end
function ModalClass(c)
    c.ModalInit = m_modalinit
    c.ModalCallback = m_modalcallback
    c.Close = m_close
end

function CallEvent(ev, ...)
    if active[ev] ~= nil then
        active[ev](active, ...)
    end
end
";
        public static InterfaceTextBundle Compile(string xmlFolder, UiXmlLoader xmlLoader, string outfolder = null)
        {
            if(outfolder != null)
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
            // uimain boilerplate
            mainlua.AppendLine(BOILERPLATE);
            // classes
            foreach (var file in CompileFiles(xmlFolder))
            {
                try
                {
                    var (classname, source) = xmlLoader.LuaClassDesigner(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
                    bundle.AddStringCompressed(classname + ".designer.lua", source);
                    if(outfolder != null)
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
            // stylesheet
            mainlua.AppendLine(xmlLoader.StylesheetToLua(File.ReadAllText(Path.Combine(xmlFolder, "stylesheet.xml"))));
            bundle.AddStringCompressed("uimain.lua", mainlua.ToString());
            if (outfolder != null)
            {
                File.WriteAllText(Path.Combine(outfolder, "uimain.lua"), mainlua.ToString());
                File.WriteAllText(Path.Combine(outfolder, "interface.json"), bundle.ToJSON());
            }
            return bundle;
        }
    }
}