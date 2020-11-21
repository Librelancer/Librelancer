// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibreLancer.Lua;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace LibreLancer.Interface
{
    public partial class LuaContext : IDisposable
    {
        Script script;
        
        private object _callevent;
        private object _openscene;
        
        static LuaContext()
        {
            UserData.DefaultAccessMode = InteropAccessMode.Hardwired;
            Lua.LuaContext_Hardwire.Initialize();
            UserData.RegisterType<HorizontalAlignment>();
            UserData.RegisterType<VerticalAlignment>();
            UserData.RegisterType<AnchorKind>();
        }
        public static void RegisterType<T>()
        {
            UserData.RegisterType<T>(InteropAccessMode.LazyOptimized);
        }

        private UiContext uiContext;

        public LuaContext(UiContext context)
        {
            uiContext = context;
            script = new Script(CoreModules.Preset_HardSandbox | CoreModules.Metatables);
            script.Options.DebugPrint = s => FLLog.Info("Lua", s);
            script.Globals["HorizontalAlignment"] = UserData.CreateStatic<HorizontalAlignment>();
            script.Globals["VerticalAlignment"] = UserData.CreateStatic<VerticalAlignment>();
            script.Globals["AnchorKind"] = UserData.CreateStatic<AnchorKind>();
            script.Options.ScriptLoader = new UiScriptLoader(context);
            var globalTable = script.Globals;
            foreach (var g in script.Globals.Keys)
            {
                globalTable[g] = script.Globals[g];
            }
            var typeTable = new Table(script);
            LuaContext_Hardwire.GenerateTypeTable(typeTable);
            globalTable["ClrTypes"] = typeTable;
            globalTable["Game"] = context.GameApi;
            //Functions
            globalTable["Funcs"] = new ContextFunctions(this);
            script.DoString(DEFAULT_LUA, null, "LuaContext.LuaCode");
        }

        public void LoadMain()
        {
            try
            {
                script.DoFile("uimain.lua");
                _callevent = script.Globals["CallEvent"];
                _openscene = script.Globals["OpenScene"];
                uiContext.Data.Stylesheet = script.Call(script.Globals["CreateStylesheet"]).ToObject<Stylesheet>();
            }
            catch (InterpreterException e)
            {
                throw new Exception($"{e.DecoratedMessage}", e);
            }
          
        }

        public void SetGameApi(object g)
        {
            script.Globals["Game"] = g;
        }

        public void OpenScene(string scene)
        {
            timers = new List<LuaTimer>();
            script.Call(_openscene, scene);
        }
        
        TimeSpan lastTime;
        public void DoTimers(TimeSpan globalTime)
        {
            if (lastTime == TimeSpan.Zero)
            {
                lastTime = globalTime;
                return;
            }
            var delta = globalTime - lastTime;
            lastTime = globalTime;
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                var t = timers[i];
                t.Time -= delta;
                if (t.Time <= TimeSpan.Zero)
                {
                    script.Call(t.Function);
                    timers.RemoveAt(i);
                }
            }
        }

        private List<LuaTimer> timers = new List<LuaTimer>();
        class LuaTimer
        {
            public TimeSpan Time;
            public object Function;
        }
        
        public void Timer(float timer, object func)
        {
            timers.Add(new LuaTimer() { Time = TimeSpan.FromSeconds(timer), Function = func});
        }
        
        [MoonSharpUserData]
        public class ContextFunctions
        {
            private LuaContext c;
            public ContextFunctions(LuaContext lc)
            {
                c = lc;
            }
            public void Timer(float time, object func) => c.Timer(time, func);
            public void PlaySound(string snd) => c.uiContext.PlaySound(snd);
            public void SetWidget(UiWidget widget) => c.uiContext.SetWidget(widget);
            public int OpenModal(UiWidget widget) => c.uiContext.OpenModal(widget);
            public void CloseModal(int handle) => c.uiContext.CloseModal(handle);
            public InterfaceColor GetColor(string col) => c.uiContext.Data.GetColor(col);
            public InterfaceModel GetModel(string mdl) => c.uiContext.Data.Resources.Models.First(x => x.Name == mdl);
            public InterfaceImage GetImage(string img) => c.uiContext.Data.Resources.Images.First(x => x.Name == img);
            public string GetNavbarIconPath(string ico) => c.uiContext.Data.GetNavbarIconPath(ico);

            Dictionary<string,DynValue> mods = new Dictionary<string, DynValue>();
            public DynValue Require(string mod)
            {
                if (mods.ContainsKey(mod))
                    return mods[mod];
                string filename = mod;
                if (!c.uiContext.Data.FileExists(filename))
                    filename += ".lua";
                var mx =  c.script.DoString(c.uiContext.Data.ReadAllText(filename), null, mod);
                mods.Add(mod, mx);
                return mx;
            }
        }
        public void CallEvent(string ev, params object[] p)
        {
            var args = new[] {(object) ev}.Concat(p).ToArray();
            script.Call(_callevent, args);
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