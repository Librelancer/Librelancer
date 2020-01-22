// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer;
using NLua;
using NLua.Exceptions;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public partial class Scene : Container
    {
        private string _switchTo = null;
        private Lua lua;
        
        [UiIgnore]
        public string SwitchToResult
        {
            get { return _switchTo; }
        }
        public void SwitchTo(string scene)
        {
            _switchTo = scene;
        }

        internal UiRecreateHandle RecreateHandle { get; set; }
        public string ScriptFile { get; set; }

        public LuaTable Env;
        private LuaFunction RunSandboxed;
        private LuaFunction CallEvent;
        private List<LuaTimer> timers = new List<LuaTimer>();
        class LuaTimer
        {
            public TimeSpan Time;
            public LuaFunction Function;
        }
        public void Reset()
        {
            DeleteLua();
            RecreateHandle.Refill();
            ApplyStyles();
        }
        public override void EnableScripting(UiContext context, string modalData)
        {
            try
            {
                DeleteLua();
                lua = new Lua();
                lua.State.Encoding = Encoding.UTF8;
                lua.LoadCLRPackage();
                lua["LogString"] = (Action<string>) LogString;
                lua["LogError"] = (Action<string>) LogError;
                lua["ReadAllText"] = (Func<string,string>)context.ReadAllText;
                lua.DoString(LUA_SANDBOX);
                if (!string.IsNullOrEmpty(modalData))
                {
                    ((LuaFunction) lua["DeserializeToEnv"]).Call("ModalData", modalData);
                }
                Env = (LuaTable) lua["Env"];
                Env["GetElement"] = (Func<string, UiWidget>) GetElement;
                Env["NewObject"] = (Func<string, object>) UiXmlReflection.Instantiate;
                Env["OpenModal"] = (Action<string, object, LuaFunction>) ((xml, data, function) =>
                {
                    OpenModal(context, xml, data, function);
                });
                Env["CloseModal"] = (Action<object>) ((table) =>
                {
                    CloseModal(context, table);
                });
                Env["Timer"] = (Action<float, LuaFunction>)((timer, func) =>
                {
                    timers.Add(new LuaTimer() { Time = TimeSpan.FromSeconds(timer), Function = func});
                });
                Env["ApplyStyles"] = (Action) ApplyStyles;
                Env["GetScene"] = (Func<Scene>) (() => this);
                Env["PlaySound"] = (Action<string>) context.PlaySound;
                Env["Game"] = context.GameApi;
                Env["Color"] = (Func<string,InterfaceColor>)context.GetColor;
                Env["GetNavbarIconPath"] = (Func<string, string>) context.GetNavbarIconPath;
                Env["SwitchTo"] = (Action<string>) (SwitchTo);
                RunSandboxed = (LuaFunction) lua["RunSandboxed"];
                CallEvent = (LuaFunction) lua["CallEvent"];
                RunSandboxed.Call(context.ReadAllText(ScriptFile), ScriptFile);
            }
            catch (LuaScriptException lse)
            {
                FLLog.Error("Lua", lse.ToString());
            }
            catch (LuaException le)
            {
                FLLog.Error("Lua", le.ToString());
            }
            catch (Exception e)
            {
                FLLog.Error("Lua", $"{e.GetType()}\n{e.Message}\n{e.StackTrace}");
                lua?.Dispose();
                lua = null;
            }
        }
        
        void CloseModal(UiContext context, object table)
        {
            string modalData = null;
            if (table != null) {
                modalData = (string)(
                    ((LuaFunction)lua["Serialize"]).Call(table)[0]
                );
            }
            context.CloseModal(modalData);
        }
        void OpenModal(UiContext context, string xml, object table, LuaFunction function)
        {
            string modalData = null;
            if (table != null) {
                modalData = (string)(
                    ((LuaFunction)lua["Serialize"]).Call(table)[0]
                );
            }
            Action<string> onClose = null;
            if (function != null)
            {
                onClose = (x) =>
                {
                    object argument = null;
                    if (!string.IsNullOrEmpty(x)) {
                        argument = ((LuaFunction) lua["Deserialize"]).Call(x)[0];
                    }
                    function.Call(argument);
                };
            }
            context.OpenModal(xml, modalData, onClose);
        }

        private TimeSpan lastTime;

        void DoTimers(TimeSpan globalTime)
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
                    t.Function.Call();
                    timers.RemoveAt(i);
                }
            }
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            DoTimers(context.GlobalTime);
            Background?.Draw(context, parentRectangle);
            base.Render(context, parentRectangle);
        }

        public override void ScriptedEvent(string ev, params object[] param)
        {
            var p = new object[param.Length + 1];
            p[0] = ev;
            for (int i = 0; i < param.Length; i++)
                p[i + 1] = param[i];
            CallEvent?.Call(p);
        }

        static void LogString(string s)
        {
            FLLog.Info("Lua", s);
        }

        static void LogError(object o)
        {
            FLLog.Error("Lua", o.ToString());
        }
        private Stylesheet currentSheet;
        void ApplyStyles()
        {
            if(currentSheet != null) ApplyStylesheet(currentSheet);
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            currentSheet = sheet;
            base.ApplyStylesheet(sheet);
        }

        void DeleteLua()
        {
            timers = new List<LuaTimer>();
            lua?.Dispose();
            lua = null;
        }

        public override void Dispose() => DeleteLua();
    }
}