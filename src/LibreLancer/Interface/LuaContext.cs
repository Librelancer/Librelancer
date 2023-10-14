// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using LibreLancer.Infocards;
using LibreLancer.Interface.WattleMaths;
using LibreLancer.Net;
using WattleScript.Interpreter;
using WattleScript.Interpreter.Interop.BasicDescriptors;
using WattleScript.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
using WattleScript.Interpreter.Loaders;

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
            LuaHardwire_LibreLancer.Initialize();
            UserData.RegisterType<HorizontalAlignment>();
            UserData.RegisterType<VerticalAlignment>();
            UserData.RegisterType<AnchorKind>();
            UserData.RegisterType<ChatCategory>();
            UserData.RegisterType(new WattleVector2());
            UserData.RegisterType(new WattleVector3());
        }

        //Run static .cctor
        public static void Initialize() { }




        public static void RegisterType<T>()
        {
            UserData.RegisterType<T>(InteropAccessMode.LazyOptimized);
        }

        private UiContext uiContext;

        public LuaContext(UiContext context)
        {
            uiContext = context;
            script = new Script(CoreModules.Preset_SoftSandboxWattle | CoreModules.LoadMethods);
            script.Options.Syntax = ScriptSyntax.Wattle;
            script.Options.DebugPrint = s => FLLog.Info("WattleScript", s);
            script.Globals["HorizontalAlignment"] = UserData.CreateStatic<HorizontalAlignment>();
            script.Globals["VerticalAlignment"] = UserData.CreateStatic<VerticalAlignment>();
            script.Globals["AnchorKind"] = UserData.CreateStatic<AnchorKind>();
            script.Options.ScriptLoader = new UiScriptLoader(context);
            var globalTable = script.Globals;
            globalTable["ScreenWidth"] = () => 480 * (context.ViewportWidth / context.ViewportHeight);
            WattleVector2.CreateTable(script);
            WattleVector3.CreateTable(script);
            var typeTable = new Table(script);
            globalTable["ClrTypes"] = typeTable;
            typeTable["System_Collections_Generic_List___LibreLancer_Interface_XmlStyle___"] = typeof(List<XmlStyle>);
            typeTable["System_Collections_Generic_List___LibreLancer_Interface_DisplayElement___"] =
                typeof(List<DisplayElement>);
            typeTable["System_Collections_Generic_List___LibreLancer_Interface_UiWidget___"] = typeof(List<UiWidget>);
            typeTable["System_Collections_Generic_List___LibreLancer_Interface_ListItem___"] =
                typeof(List<ListItem>);
            typeTable["System_Collections_Generic_List___LibreLancer_Interface_TableColumn___"] =
                typeof(List<TableColumn>);

            foreach (var type in typeof(LuaContext).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(false).OfType<WattleScriptUserDataAttribute>().Any())
                {
                    typeTable[type.FullName.Replace(".", "_")] = type;
                }
            }
            globalTable["Game"] = context.GameApi;
            globalTable["Events"] = DynValue.NewTable(script);
            //Functions
            globalTable["Funcs"] = new ContextFunctions(this);
            StringBuilder globalsCode = new StringBuilder();
            globalsCode.AppendLine("local _f = Funcs");
            foreach (var method in typeof(ContextFunctions).GetMethods())
            {
                if (method.Name == "GetType" ||
                    method.Name == "ToString" ||
                    method.Name == "Equals" ||
                    method.Name == "GetHashCode")
                    continue;
                globalsCode.Append("function ").Append(method.Name).AppendLine("(...)");
                globalsCode.Append("    return _f.").Append(method.Name).AppendLine("(...)");
                globalsCode.AppendLine("end");
            }
            globalsCode.AppendLine("Funcs = nil");
            script.DoString(globalsCode.ToString(), null, "context functions");
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

        double lastTime;
        public void DoTimers(double globalTime)
        {
            if (lastTime == 0)
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
                if (t.Time <= 0)
                {
                    script.Call(t.Function);
                    timers.RemoveAt(i);
                }
            }
        }

        private List<LuaTimer> timers = new List<LuaTimer>();
        class LuaTimer
        {
            public double Time;
            public object Function;
        }

        public void Timer(float timer, object func)
        {
            timers.Add(new LuaTimer() { Time = timer, Function = func});
        }

        [WattleScriptUserData]
        public class ContextFunctions
        {
            private LuaContext c;
            public ContextFunctions(LuaContext lc)
            {
                c = lc;
            }
            public void Timer(float time, object func) => c.Timer(time, func);
            public void PlaySound(string snd) => c.uiContext.PlaySound(snd);
            public void PlayVoiceLine(string voice, string line) => c.uiContext.PlayVoiceLine(voice, line);
            public void SetWidget(UiWidget widget) => c.uiContext.SetWidget(widget);
            public int OpenModalWidget(UiWidget widget) => c.uiContext.OpenModal(widget);
            public void SwapModalWidget(UiWidget widget, int handle) => c.uiContext.SwapModal(widget, handle);
            public void CloseModal(int handle) => c.uiContext.CloseModal(handle);
            public InterfaceColor GetColor(string col) => c.uiContext.Data.GetColor(col);
            public InterfaceModel GetModel(string mdl) => c.uiContext.Data.Resources.Models.First(x => x.Name == mdl);
            public InterfaceImage GetImage(string img) => c.uiContext.Data.Resources.Images.First(x => x.Name == img);
            public string GetNavbarIconPath(string ico) => c.uiContext.Data.GetNavbarIconPath(ico);
            public Vector3 Vector3(float x, float y, float z) => new Vector3(x, y, z);

            public string StringFromID(int id) => c.uiContext.Data.Infocards.GetStringResource(id);
            public Infocard GetInfocard(int id) =>
                RDLParse.Parse(c.uiContext.Data.Infocards.GetXmlResource(id), c.uiContext.Data.Fonts);
            public string NumberToStringCS(double num, string fmt) => num.ToString(fmt);
        }
        public void CallEvent(string ev, params object[] p)
        {
            try
            {
                var args = new[] {(object) ev}.Concat(p).ToArray();
                script.Call(_callevent, args);
            }
            catch (ScriptRuntimeException ex)
            {
                throw new Exception(ex.DecoratedMessage, ex);
            }
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
                return context.Data.FileExists(name);
            }
            public override object LoadFile(string file, Table globalContext)
            {
                return context.Data.ReadAllText(file);
            }
            public override string ResolveModuleName(string modname, Table globalContext)
            {
                if (context.Data.FileExists(modname)) return modname;
                if (context.Data.FileExists(modname + ".lua")) return modname + ".lua";
                return modname;
            }
        }
    }
}
