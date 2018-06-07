/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Neo.IronLua;
using LibreLancer.Media;
namespace LibreLancer
{
    public class LuaStyleEnvironment
    {
        public static Lua L;
        public static LuaGlobalPortable G;

        static LuaStyleEnvironment()
        {
            L = new Lua();
            G = L.CreateEnvironment();
            G.Add("mixcolor", (Func<float, string, string, Color4>)MixColor);
            G.Add("color", (Func<string, Color4>)Color);
        }
        public static void Do(LuaChunk c, object e, float time)
        {
            var g = (dynamic)G;
            g.e = e;
            g.time = time;
            G.DoChunk(c);
        }

        public static Color4 MixColor(float v, string sc1, string sc2)
        {
            var c1 = XInt.Parser.Color(sc1);
            var c2 = XInt.Parser.Color(sc2);
            var rgb = Utf.Ale.AlchemyEasing.EaseColorRGB(Utf.Ale.EasingTypes.Linear, v, 0, 1, c1.Rgb, c2.Rgb);
            return new Color4(
                rgb,
                Utf.Ale.AlchemyEasing.Ease(Utf.Ale.EasingTypes.Linear, v, 0, 1, c1.A, c2.A)
            );
        }
        public static Color4 Color(string s)
        {
            return XInt.Parser.Color(s);
        }
    }

    public class XmlUIManager : IDisposable
    {
        public FreelancerGame Game;

        XInterface xml;
        Lua lua;
        LuaGlobalPortable env;
        dynamic _g;
        public List<XmlUIElement> Elements = new List<XmlUIElement>();
        public Font Font;
        public double AnimationFinishTimer;

        string apiname;
        object api;

        public XmlUIManager(FreelancerGame game, string apiname, object api, string src)
        {
            Game = game;
            this.apiname = apiname;
            this.api = api;
            xml = XInterface.Load(src);
            Font = game.Fonts.GetSystemFont("Agency FB");
            foreach (var file in xml.ResourceFiles)
                game.ResourceManager.LoadResourceFile(game.GameData.ResolveDataPath(file.Substring(2)));
            LoadScene(xml.DefaultScene);
        }
        void LoadScene(string id)
        {
            if (lua != null) lua.Dispose();
            Elements = new List<XmlUIElement>();
            lua = new Lua();
            env = lua.CreateEnvironment();
            env.DoChunk("events = {}", "$internal");
            env.Add(apiname, api);
            env.Add("dom", new LuaDom(this));
            env.Add("sound", new LuaSound(this));
            _g = (dynamic)env;
            var scn = xml.Scenes.Where((x) => x.ID == id).First();
            if(scn.Scripts != null)
            foreach (var script in scn.Scripts)
                env.DoChunk(script, "$xml");
            if(scn.Buttons != null)
            foreach (var button in scn.Buttons)
                Elements.Add(new XmlUIButton(this, button, xml.Styles.Where((x) => x.ID == button.Style).First()));
            if(scn.Images != null)
            foreach (var img in scn.Images)
                Elements.Add(new XmlUIImage(img, this));
            if(scn.Panels != null)
            foreach (var pnl in scn.Panels)
                Elements.Add(new XmlUIPanel(pnl, xml.Styles.Where((x) => x.ID == pnl.Style).First(), this));
        }
        void SwapIn(string id)
        {
            LoadScene(id);
            Enter();
        }

        Dictionary<string, SoundData> sounds = new Dictionary<string, SoundData>();
        void PlaySound(string name)
        {
            SoundData dat;
            if (!sounds.TryGetValue(name, out dat))
            {
                dat = Game.Audio.AllocateData();
                dat.LoadFile(Game.GameData.GetMusicPath(name));
                sounds.Add(name, dat);
            }
            Game.Audio.PlaySound(dat);
        }
        class LuaSound
        {
            XmlUIManager manager;
            public LuaSound(XmlUIManager manager) => this.manager = manager;
            public void play(string id) => manager.PlaySound(id);
        }
        string afteranimation_lua;
        Action afteranimation_action;
        class LuaDom
        {
            XmlUIManager manager;
            public LuaDom(XmlUIManager manager) => this.manager = manager;
            public XmlUIElement.LuaAPI element(string id)
            {
                return manager.Elements.Where((x) => x.ID == id).First().Lua;
            }
            public void afteranimation(string snippet)
            {
                manager.afteranimation_lua = snippet;
            }
            public void changeto(string id)
            {
                manager.Leave();
                manager.afteranimation_action = () =>
                {
                    manager.SwapIn(id);
                };
            }
        }

        public void Enter()
        {
            if (_g.events["onentry"] != null)
                _g.events.onentry();
        }
        public void Leave()
        {
            if (_g.events["onleave"] != null)
                _g.events.onleave();
        }
        public void Leave(Action after)
        {
            afteranimation_action = after;
            if (_g.events["onleave"] != null)
                _g.events.onleave();
        }
        public void Call(string s)
        {
            env.DoChunk(s, "$internal");
        }
        void After()
        {
            if (afteranimation_lua != null)
            {
                env.DoChunk(afteranimation_lua, "$afteranimation");
                afteranimation_lua = null;
            }
            if (afteranimation_action != null)
            {
                afteranimation_action();
                afteranimation_action = null;
            }
        }
        public void Update(TimeSpan delta)
        {
            if(AnimationFinishTimer > 0) {
                AnimationFinishTimer -= delta.TotalSeconds;
                if (AnimationFinishTimer <= 0)
                    After();
            } else {
                After();
            }
            foreach (var elem in Elements)
                elem.Update(delta);
        }

        public void Draw(TimeSpan delta)
        {
            Game.RenderState.DepthEnabled = false;
            foreach (var elem in Elements)
                elem.Draw(delta);
        }

        public void Dispose()
        {
            lua.Dispose();
            foreach (var v in sounds.Values)
                v.Dispose();
        }
    }
}
