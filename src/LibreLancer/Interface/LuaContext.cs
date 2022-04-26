// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer.Infocards;
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
            UserData.RegisterType(new Vector2Lua());
            UserData.RegisterType(new Vector3Lua());
        }

        class Vector2Lua : HardwiredUserDataDescriptor
        {
            public Vector2Lua() : base(typeof(Vector2))
            {
                AddMember("X", new DescX());
                AddMember("Y", new DescY());
            }

            class DescX : HardwiredMemberDescriptor
            {
                public DescX() : 
                    base(typeof(float), "X", 
                        false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                {
                }

                protected override object GetValueImpl(Script script, object obj)
                {
                    return Unsafe.Unbox<Vector2>(obj).X;
                }

                protected override void SetValueImpl(Script script, object obj, object value)
                {
                    Unsafe.Unbox<Vector2>(obj).X = (float)value;
                }
            }
            
            class DescY : HardwiredMemberDescriptor
            {
                public DescY() : 
                    base(typeof(float), "X", 
                        false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                {
                }

                protected override object GetValueImpl(Script script, object obj)
                {
                    return Unsafe.Unbox<Vector2>(obj).Y;
                }

                protected override void SetValueImpl(Script script, object obj, object value)
                {
                    Unsafe.Unbox<Vector2>(obj).Y = (float)value;
                }
            }
            
        }

        class Vector3Lua : HardwiredUserDataDescriptor
        {
            public Vector3Lua() : base(typeof(Vector3))
            {
                AddMember("X", new DescX());
                AddMember("Y", new DescY());
                AddMember("Z", new DescZ());
            }
            
            class DescX : HardwiredMemberDescriptor
            {
                public DescX() : 
                    base(typeof(float), "X", 
                        false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite) 
                {
                }

                protected override object GetValueImpl(Script script, object obj)
                {
                    return Unsafe.Unbox<Vector3>(obj).X;
                }

                protected override void SetValueImpl(Script script, object obj, object value)
                {
                    Unsafe.Unbox<Vector3>(obj).X = (float)value;
                }
            }
            
            class DescY : HardwiredMemberDescriptor
            {
                public DescY() : 
                    base(typeof(float), "Y", 
                        false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                {
                }

                protected override object GetValueImpl(Script script, object obj)
                {
                    return Unsafe.Unbox<Vector3>(obj).Y;
                }

                protected override void SetValueImpl(Script script, object obj, object value)
                {
                    Unsafe.Unbox<Vector3>(obj).Y = (float)value;
                }
            }
            
            class DescZ : HardwiredMemberDescriptor
            {
                public DescZ() : 
                    base(typeof(float), "Z", 
                        false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
                {
                }

                protected override object GetValueImpl(Script script, object obj)
                {
                    return Unsafe.Unbox<Vector3>(obj).Z;
                }

                protected override void SetValueImpl(Script script, object obj, object value)
                {
                    Unsafe.Unbox<Vector3>(obj).Z = (float)value;
                }
            }
            
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
            public int OpenModal(UiWidget widget) => c.uiContext.OpenModal(widget);
            public void SwapModal(UiWidget widget, int handle) => c.uiContext.SwapModal(widget, handle);
            public void CloseModal(int handle) => c.uiContext.CloseModal(handle);
            public InterfaceColor GetColor(string col) => c.uiContext.Data.GetColor(col);
            public InterfaceModel GetModel(string mdl) => c.uiContext.Data.Resources.Models.First(x => x.Name == mdl);
            public InterfaceImage GetImage(string img) => c.uiContext.Data.Resources.Images.First(x => x.Name == img);
            public string GetNavbarIconPath(string ico) => c.uiContext.Data.GetNavbarIconPath(ico);
            public Vector3 Vector3(float x, float y, float z) => new Vector3(x, y, z);

            public string StringFromID(int id)
            {
                var str = c.uiContext.Data.Infocards.GetStringResource(id);
                if (str.EndsWith("%M")) return str.Substring(0, str.Length - 2).ToUpper();
                else return str;
            }
            public Infocard GetInfocard(int id) =>
                RDLParse.Parse(c.uiContext.Data.Infocards.GetXmlResource(id), c.uiContext.Data.Fonts);
            public string NumberToStringCS(double num, string fmt) => num.ToString(fmt);

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