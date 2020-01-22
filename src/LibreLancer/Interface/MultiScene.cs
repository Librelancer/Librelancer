// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class MultiScene : UiWidget
    {
        [UiContent]
        public List<Scene> Scenes { get; set; } = new List<Scene>();
        public string ActiveScene { get; set; }

        private Scene _activeScene;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            SetActiveScene();
            if (_activeScene != null) {
                _activeScene.Render(context, parentRectangle);
                if (!string.IsNullOrWhiteSpace(_activeScene.SwitchToResult))
                {
                    ActiveScene = _activeScene.SwitchToResult;
                    _activeScene.SwitchTo(null);
                }
            }
        }

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            foreach(var scn in Scenes) scn.ApplyStylesheet(sheet);
        }

        private bool scriptingEnabled = false;
        private string modalData = null;
        private UiContext scriptingContext;
        
        public override void EnableScripting(UiContext context, string modalData)
        {
            scriptingContext = context;
            scriptingEnabled = true;
            this.modalData = modalData;
            SetActiveScene();
        }

        private bool lastScriptingEnabled = false;
        void SetActiveScene()
        {
            if (_activeScene != null && _activeScene.ID.Equals(ActiveScene, StringComparison.OrdinalIgnoreCase)) {
                if (scriptingEnabled && !lastScriptingEnabled)
                {
                    lastScriptingEnabled = true;
                    _activeScene.EnableScripting(scriptingContext, modalData);
                }
                return;
            }
            if(string.IsNullOrWhiteSpace(ActiveScene)) {
                _activeScene = null;
                return;
            }
            _activeScene = Scenes.FirstOrDefault(x => x.ID.Equals(ActiveScene, StringComparison.OrdinalIgnoreCase));
            _activeScene.Reset();
            if (scriptingEnabled)
            {
                _activeScene.EnableScripting(scriptingContext, modalData);
                lastScriptingEnabled = true;
            }
        }
        
        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            SetActiveScene();
            _activeScene?.OnMouseClick(context, parentRectangle);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            SetActiveScene();
            _activeScene?.OnMouseDown(context, parentRectangle);
        }

        public override void ScriptedEvent(string ev, params object[] param)
        {
            SetActiveScene();
            _activeScene?.ScriptedEvent(ev, param);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            SetActiveScene();
            _activeScene?.OnMouseUp(context, parentRectangle);
        }
    }
}