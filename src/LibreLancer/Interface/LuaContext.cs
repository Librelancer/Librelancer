// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Interface.Reflection;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Serialization;

namespace LibreLancer.Interface
{
    public partial class LuaContext : IDisposable
    {
        public Scene Scene;
        static Script script;
        private static byte[] baseCode;
        private Table globalTable;

        private object _serialize;
        private object _deserialize;
        private object _callevent;

        static LuaContext()
        {
            UserData.DefaultAccessMode = InteropAccessMode.Hardwired;
            Lua.LuaContext_Hardwire.Initialize();
            UserData.RegisterType<HorizontalAlignment>();
            UserData.RegisterType<VerticalAlignment>();
            UserData.RegisterType<AnchorKind>();
            script = new Script(CoreModules.Preset_HardSandbox);
            script.Options.DebugPrint = s => FLLog.Info("Lua", s);
            script.Globals["HorizontalAlignment"] = UserData.CreateStatic<HorizontalAlignment>();
            script.Globals["VerticalAlignment"] = UserData.CreateStatic<VerticalAlignment>();
            script.Globals["AnchorKind"] = UserData.CreateStatic<AnchorKind>();
            baseCode = Compile(script, null, DEFAULT_LUA, "LuaContext.LuaCode");
        }
        public static void RegisterType<T>()
        {
            UserData.RegisterType<T>(InteropAccessMode.LazyOptimized);
        }

        static byte[] Compile(Script sc, Table globalTable, string code, string name)
        {
            DynValue v1 = sc.LoadString(code, globalTable, name);
            using (var stream = new MemoryStream())
            {
                sc.Dump(v1, stream);
                return stream.ToArray();
            }
        }

        static void RunBytes(Script script, Table globalTable, byte[] bytes)
        {
            try
            {
                using (var stream = new MemoryStream(bytes, false)) {
                    script.LoadStream(stream, globalTable).Function.Call();
                }
            }
            catch (ScriptRuntimeException e)
            {
                throw new Exception(e.DecoratedMessage);
            }
           
        }

        private UiContext uiContext;
        public LuaContext(UiContext context, Scene scene)
        {
            uiContext = context;
            script.Options.ScriptLoader = new UiScriptLoader(context);
            globalTable = new Table(script);
            foreach (var g in script.Globals.Keys)
            {
                globalTable[g] = script.Globals[g];
            }
            globalTable["Game"] = context.GameApi;
            //Functions
            globalTable["Funcs"] = new ContextFunctions(this);
            RunBytes(script, globalTable, baseCode);
            _serialize = globalTable["Serialize"];
            _callevent = globalTable["CallEvent"];
        }

        [MoonSharpUserData]
        public class ContextFunctions
        {
            private LuaContext c;
            public ContextFunctions(LuaContext lc)
            {
                c = lc;
            }
            public UiWidget GetElement(string e) => c.Scene.GetElement(e);
            public object NewObject(string obj) => UiXmlReflection.Instantiate(obj);
            public void OpenModal(string xml, DynValue data, object function) => c.OpenModal(c.uiContext, xml, data, function);
            public void CloseModal(DynValue table) => c.CloseModal(c.uiContext, table);
            public void Timer(float time, object func) => c.Scene.Timer(time, func);
            public void ApplyStyles() => c.Scene.ApplyStyles();
            public Scene GetScene() => c.Scene;
            public void PlaySound(string snd) => c.uiContext.PlaySound(snd);
            public InterfaceColor Color(string col) => c.uiContext.Data.GetColor(col);
            public string GetNavbarIconPath(string ico) => c.uiContext.Data.GetNavbarIconPath(ico);
            public void SwitchTo(string scn) => c.Scene.SwitchTo(scn);
            public string SceneID() => c.Scene.ID;

            Dictionary<string,DynValue> mods = new Dictionary<string, DynValue>();
            public DynValue Require(string mod)
            {
                if (mods.ContainsKey(mod))
                    return mods[mod];
                var mx =  script.DoString(c.uiContext.Data.ReadAllText(mod), c.globalTable, mod);
                mods.Add(mod, mx);
                return mx;
            }
        }
        void CloseModal(UiContext context, DynValue table)
        {
            string modalData = null;
            if (table != null)
            {
                modalData = Serialize(table);
            }
            context.CloseModal(modalData);
        }
        void OpenModal(UiContext context, string xml, DynValue table, object function)
        {
            string modalData = null;
            if (table != null)
            {
                modalData = Serialize(table);
            }
            Action<string> onClose = null;
            if (function != null)
            {
                onClose = (x) =>
                {
                    DynValue argument = null;
                    if (!string.IsNullOrEmpty(x))
                    {
                        argument = script.DoString("return " + x);
                    }
                    script.Call(function, argument);
                };
            }
            context.OpenModal(xml, modalData, onClose);
        }
        public void Invoke(object func)
        {
            script.Call(func);
        }
        static Dictionary<string,byte[]> bytes = new Dictionary<string, byte[]>();

        public void DoFile(string filename)
        {
            if (!bytes.TryGetValue(filename, out var code))
            {
                code = Compile(script, globalTable, uiContext.Data.ReadAllText(filename), filename);
                bytes.Add(filename, code);
                try
                {
                    script.DoFile(filename, globalTable);
                }
                catch (ScriptRuntimeException e)
                {
                    throw new Exception(e.DecoratedMessage);
                }
            } else
                RunBytes(script, globalTable, code);
        }
        public void CallEvent(string ev, params object[] p)
        {
            var args = new[] {(object) ev}.Concat(p).ToArray();
            script.Call(_callevent, args);
        }
        string Serialize(DynValue obj)
        {
            return script.Call(_serialize, obj).String;
        }

        public void Assign(string name, string val)
        {
            script.DoString($"{name} = {val}");
        }

        public void Dispose()
        {
        }

        class UiScriptLoader : ScriptLoaderBase
        {
            private UiContext context;
            public UiScriptLoader(UiContext ctx)
            {
                context = ctx;
            }
            public override bool ScriptFileExists(string name)
            {
                return true;
            }
            public override object LoadFile(string file, Table globalContext)
            {
                return context.Data.ReadAllText(file);
            }
            public override string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}