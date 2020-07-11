// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public partial class Scene : Container
    {
        private string _switchTo = null;
        
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

        private List<LuaTimer> timers = new List<LuaTimer>();
        class LuaTimer
        {
            public TimeSpan Time;
            public object Function;
        }
        public void Reset()
        {
            RecreateHandle.Refill(this);
            ApplyStyles();
        }

        private LuaContext lua;

        public override void EnableScripting(UiContext context, string modalData)
        {
            DeleteLua();
            lua = new LuaContext(context, this);
            if (!string.IsNullOrEmpty(modalData))
                lua.Assign("ModalData", modalData);
            lua.Scene = this;
            lua.DoFile(ScriptFile);
        }

        public void Timer(float timer, object func)
        {
            timers.Add(new LuaTimer() { Time = TimeSpan.FromSeconds(timer), Function = func});
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
                    lua.Invoke(t.Function);
                    timers.RemoveAt(i);
                }
            }
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            DoTimers(context.GlobalTime);
            ScriptedEvent("Update");
            Background?.Draw(context, parentRectangle);
            base.Render(context, parentRectangle);
        }

        public override void ScriptedEvent(string ev, params object[] param)
        {
            lua?.CallEvent(ev, param);
        }
        
       
        private Stylesheet currentSheet;
        public void ApplyStyles()
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