// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
            RegisterFuncs(G);
        }
        public static void RegisterFuncs(LuaGlobalPortable gp)
        {
            gp.Add("mixcolor", (Func<float, string, string, Color4>)MixColor);
            gp.Add("color", (Func<string, Color4>)Color);
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
        List<XInt.Style> styles = new List<XInt.Style>();
        Lua lua;
        LuaGlobalPortable env;
        dynamic _g;
        public List<XmlUIElement> Elements = new List<XmlUIElement>();
        List<XmlUIElement> toadd = new List<XmlUIElement>();
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
            if (xml.ResourceFiles != null)
                foreach (var file in xml.ResourceFiles)
                    game.ResourceManager.LoadResourceFile(game.GameData.ResolveDataPath(file.Substring(2)));
            DoStyles(xml);
            LoadScene(xml.DefaultScene);
            game.Mouse.MouseDown += Mouse_MouseDown;
            game.Mouse.MouseUp += Mouse_MouseUp;
        }

        void Mouse_MouseDown(MouseEventArgs e) { if(e.Buttons == MouseButtons.Left) foreach(var el in Elements) el.OnMouseDown(); }
        void Mouse_MouseUp(MouseEventArgs e) { if(e.Buttons == MouseButtons.Left) foreach (var el in Elements) el.OnMouseUp(); }

        void DoStyles(XInterface x)
        {
            if (x.Styles != null)
                styles.AddRange(x.Styles);
            if(x.Includes != null) {
                foreach (var inc in x.Includes)
                    DoStyles(XInterface.Load(Game.GameData.GetInterfaceXml(inc.File)));
            }
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
            LuaStyleEnvironment.RegisterFuncs(env);
            _g = (dynamic)env;
            var scn = xml.Scenes.Where((x) => x.ID == id).First();
            if (scn.Scripts != null)
                foreach (var script in scn.Scripts)
                    env.DoChunk(script, "$xml");
            foreach (var item in scn.Items)
            {
                if (item is XInt.Button)
                {
                    var btn = (XInt.Button)item;
                    Elements.Add(new XmlUIButton(this, btn, styles.Where((x) => x.ID == btn.Style).First()));
                }
                else if (item is XInt.Image)
                {
                    Elements.Add(new XmlUIImage((XInt.Image)item, this));
                }
                else if (item is XInt.ServerList)
                {
                    var sl = (XInt.ServerList)item;
                    Elements.Add(new XmlUIServerList(sl, styles.Where((x) => x.ID == sl.Style).First(), this));
                }
                else if (item is XInt.Panel)
                {
                    var pnl = (XInt.Panel)item;
                    Elements.Add(new XmlUIPanel(pnl, styles.Where((x) => x.ID == pnl.Style).First(), this));
                }
                else if (item is XInt.ChatBox)
                {
                    var cb = (XInt.ChatBox)item;
                    Elements.Add(new XmlChatBox(cb, styles.Where((x) => x.ID == cb.Style).First(), this));
                }

            }
        }
        void SwapIn(string id)
        {
            LoadScene(id);
            OnConstruct();
            Enter();
        }
        public void OnConstruct()
        {
            if (_g.events["onconstruct"] != null)
                _g.events.onconstruct();
            foreach (var ctrl in toadd)
                Elements.Add(ctrl);
            toadd.Clear();
        }
        Dictionary<string, SoundData> sounds = new Dictionary<string, SoundData>();
        void PlaySound(string name)
        {
            SoundData dat;
            if (!sounds.TryGetValue(name, out dat))
            {
                dat = Game.Audio.AllocateData();
                dat.LoadFile(Game.GameData.GetAudioPath(name));
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
            public XmlUIButton.LuaAPI addbutton(dynamic dn)
            {
                var btn = new XInt.Button();
                var style = new XInt.Style();
                style.Size = new XInt.StyleSize();
                if (dn["x"] != null) btn.XText = dn.x;
                if (dn["y"] != null) btn.YText = dn.y;
                if (dn["anchor"] != null) btn.Anchor = Enum.Parse(typeof(XInt.Anchor), dn.anchor);
                if (dn["height"] != null) style.Size.HeightText = dn.height;
                if (dn["ratio"] != null) style.Size.Ratio = (float)dn.ratio;
                if (dn["scissor"] != null) style.Scissor = (bool)dn.scissor;
                if (dn["onclick"] != null) btn.OnClick = dn.onclick;
                if (dn["background"] != null) style.Background = new XInt.StyleBackground() { ColorText = dn.background };
                style.HoverStyle = dn.hoverstyle;
                if (dn["models"] != null) {
                    var mdlxml = new List<XInt.Model>();
                    foreach(var kv in dn.models) {
                        var mdl = kv.Value;
                        mdlxml.Add(new XInt.Model()
                        {
                            Path = mdl.path,
                            TransformString = mdl.transform,
                            ColorText = mdl.color
                        });
                    }
                    style.Models = mdlxml.ToArray();
                }
                var result = new XmlUIButton(manager, btn, style);
                manager.toadd.Add(result);
                return result.Lua;
            }
        }

        public dynamic Events {
            get {
                return _g.events;
            }
        }

        public void TableInsert(LuaTable table, object o)
        {
            _g.table.insert(table, o);
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
            if (_g.events["onupdate"] != null)
                _g.events.onupdate();
            foreach (var elem in Elements)
                elem.Update(delta);
            foreach (var elem in toadd)
                Elements.Add(elem);
            toadd.Clear();
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
