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

    public class XmlUIScene : IDisposable
    {
        public List<XmlUIElement> Elements = new List<XmlUIElement>();
        internal List<XmlUIElement> toadd = new List<XmlUIElement>();
        public Lua lua;
        public dynamic _g;
        public LuaGlobalPortable env;
        public double AnimationFinishTimer;
        public XmlUIManager Manager;
        //Convenience
        public Renderer2D Renderer2D => Manager.Game.Renderer2D;
        public int GWidth => Manager.Game.Width;
        public int GHeight => Manager.Game.Height;
        public int MouseX => Manager.Game.Mouse.X;
        public int MouseY => Manager.Game.Mouse.Y;
        public bool MouseDown(MouseButtons buttons) => Manager.Game.Mouse.IsButtonDown(buttons);
        public XmlUITextBox Focus;
        public void Call(string s)
        {
            env.DoChunk(s, "$internal");
        }
        public XmlUIScene(XmlUIManager manager)
        {
            Manager = manager;
        }
        public void Dispose()
        {
            lua.Dispose();
        }
        public string afteranimation_lua;
        public Action afteranimation_action;
        public void After()
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
    }

    public class XmlUIManager : IDisposable
    {
        public FreelancerGame Game;

        XInterface xml;
        List<XInt.Style> styles = new List<XInt.Style>();
        public Stack<XmlUIScene> Scenes = new Stack<XmlUIScene>();
        public List<XmlUIElement> Elements
        {
            get
            {
                return Scenes.Peek().Elements;
            }
        }
        Font defaultFont;
        Dictionary<string, Font> uiFonts = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);
        public Font GetFont(string name)
        {
            if (name[0] == '$')
            {
                Font font;
                if (!uiFonts.TryGetValue(name.Substring(1), out font)) return defaultFont;
                return font;
            }
            else
                return Game.Fonts.GetSystemFont(name);
        }

        string apiname;
        object api;

        public XmlUIManager(FreelancerGame game, string apiname, object api, string src)
        {
            Game = game;
            this.apiname = apiname;
            this.api = api;
            xml = XInterface.Load(src);
            defaultFont = game.Fonts.GetSystemFont("Arial");
            foreach(var fnt in game.GameData.Ini.Fonts.UIFonts)
            {
                uiFonts.Add(fnt.Nickname, game.Fonts.GetSystemFont(fnt.Font));
            }
            if (xml.ResourceFiles != null)
                foreach (var file in xml.ResourceFiles)
                    game.ResourceManager.LoadResourceFile(game.GameData.ResolveDataPath(file.Substring(2)));
            DoStyles(xml);
            LoadScene(xml.DefaultScene);
            game.Mouse.MouseDown += Mouse_MouseDown;
            game.Mouse.MouseUp += Mouse_MouseUp;
            game.Keyboard.TextInput += Keyboard_TextInput;
            game.Keyboard.KeyDown += Keyboard_KeyDown;
        }

        void Mouse_MouseDown(MouseEventArgs e) { if(e.Buttons == MouseButtons.Left) foreach(var el in Elements) el.OnMouseDown(); }
        void Mouse_MouseUp(MouseEventArgs e) { if(e.Buttons == MouseButtons.Left) foreach (var el in Elements) el.OnMouseUp(); }

        void Keyboard_TextInput(string text)
        {
            foreach (var e in Elements)
                e.OnTextEntered(text);
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            if(e.Key == Keys.Backspace ||
                e.Key == Keys.KeypadBackspace)
            foreach(var k in Elements)
            {
                k.OnBackspace();
            }
        }


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
            stackChanged = true;
            var scene = new XmlUIScene(this);
            Scenes.Push(scene);
            scene.lua = new Lua();
            scene.env = scene.lua.CreateEnvironment();
            scene.env.DoChunk("events = {}", "$internal");
            scene.env.Add(apiname, api);
            scene.env.Add("dom", new LuaDom(this, scene));
            scene.env.Add("sound", new LuaSound(this));
            LuaStyleEnvironment.RegisterFuncs(scene.env);
            scene._g = (dynamic)scene.env;
            var scn = xml.Scenes.Where((x) => x.ID == id).First();
            if (scn.Scripts != null)
                foreach (var script in scn.Scripts)
                    scene.env.DoChunk(script, "$xml");
            foreach (var item in scn.Items)
            {
                if (item is XInt.Button)
                {
                    var btn = (XInt.Button)item;
                    Elements.Add(new XmlUIButton(scene, btn, styles.Where((x) => x.ID == btn.Style).First()));
                }
                else if (item is XInt.Image)
                {
                    Elements.Add(new XmlUIImage((XInt.Image)item, scene));
                }
                else if (item is XInt.ServerList)
                {
                    var sl = (XInt.ServerList)item;
                    Elements.Add(new XmlUIServerList(sl, styles.Where((x) => x.ID == sl.Style).First(), scene));
                }
                else if (item is XInt.CharacterList)
                {
                    var cl = (XInt.CharacterList)item;
                    Elements.Add(new XmlUICharacterList(cl, styles.Where(x => x.ID == cl.Style).First(), scene));
                }
                else if (item is XInt.TextBox)
                {
                    var tx = (XInt.TextBox)item;
                    Elements.Add(new XmlUITextBox(tx, styles.Where((x) => x.ID == tx.Style).First(), scene));
                }
                else if (item is XInt.ChatBox)
                {
                    var cb = (XInt.ChatBox)item;
                    Elements.Add(new XmlChatBox(cb, styles.Where((x) => x.ID == cb.Style).First(), scene));
                }
                else if (item is XInt.Panel)
                {
                    var pnl = (XInt.Panel)item;
                    Elements.Add(new XmlUIPanel(pnl, styles.Where((x) => x.ID == pnl.Style).First(), scene));
                }

               
            }
        }

        void SwapIn(string id)
        {
            Scenes.Pop();
            LoadScene(id);
            OnConstruct();
            Enter();
        }

        void StackNew(string id)
        {
            LoadScene(id);
            OnConstruct();
            Enter();
        }

        void StackPop()
        {
            stackChanged = true;
            var scn = Scenes.Peek();
            scn.Dispose();
            Scenes.Pop();
        }

        public void OnConstruct()
        {
            var scn = Scenes.Peek();
            if (scn._g.events["onconstruct"] != null)
                scn._g.events.onconstruct();
            foreach (var ctrl in scn.toadd)
                scn.Elements.Add(ctrl);
            scn.toadd.Clear();
        }

        public void CallEvent(string ev)
        {
            dynamic evmethod = null;
            foreach(var scn in Scenes)
            {
                if(scn._g["events"] != null)
                {
                    if (scn._g.events[ev] != null)
                    {
                        evmethod = scn._g.events[ev];
                        break;
                    }
                }
            }
            evmethod?.Invoke();
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

        class LuaDom
        {
            XmlUIManager manager;
            XmlUIScene scn;
            public LuaDom(XmlUIManager manager, XmlUIScene scn)
            {
                this.manager = manager;
                this.scn = scn;
            }
            public XmlUIElement.LuaAPI element(string id)
            {
                return scn.Elements.Where((x) => x.ID == id).First().Lua;
            }
            public void afteranimation(string snippet)
            {
                scn.afteranimation_lua = snippet;
            }
            public void changeto(string id)
            {
                manager.Leave();
                scn.afteranimation_action = () =>
                {
                    manager.SwapIn(id);
                };
            }
            public void dialog(string id)
            {
                manager.StackNew(id);
            }
            public void close(string id)
            {
                manager.Leave();
                scn.afteranimation_action = () =>
                {
                    manager.StackPop();
                };
            }
            public float globaltime()
            {
                return (float)manager.Game.TotalTime;
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
                var result = new XmlUIButton(scn, btn, style);
                scn.toadd.Add(result);
                return result.Lua;
            }
        }

        public dynamic Events {
            get {
                var scn = Scenes.Peek();
                return scn._g.events;
            }
        }

        public void TableInsert(LuaTable table, object o)
        {
            var scn = Scenes.Peek();
            scn._g.table.insert(table, o);
        }

        public void Enter()
        {
            var scn = Scenes.Peek();
            if (scn._g.events["onentry"] != null)
                scn._g.events.onentry();
        }

        public void Leave()
        {
            var scn = Scenes.Peek();
            if (scn._g.events["onleave"] != null)
                scn._g.events.onleave();
        }
        public void Leave(Action after)
        {
            var scn = Scenes.Peek();
            scn.afteranimation_action = after;
            if (scn._g.events["onleave"] != null)
                scn._g.events.onleave();
        }

        bool stackChanged = false;
        bool modifiedTextInput = false;
        public void Update(TimeSpan delta)
        {
            stackChanged = false;
            var cscn = Scenes.Peek();
            foreach (var scene in Scenes)
            {
                if (scene.AnimationFinishTimer > 0)
                {
                    scene.AnimationFinishTimer -= delta.TotalSeconds;
                    if (scene.AnimationFinishTimer <= 0)
                        scene.After();
                }
                else
                {
                    scene.After();
                }
                if (scene._g.events["onupdate"] != null)
                    scene._g.events.onupdate();
                foreach (var item in scene.Elements)
                    item.Update(delta, scene == cscn);
                foreach (var item in scene.toadd)
                    scene.Elements.Add(item);
                scene.toadd.Clear();
                if (stackChanged) return;
            }
            if (cscn.Focus != null)
            {
                Game.EnableTextInput();
                modifiedTextInput = true;
            }
            else if (modifiedTextInput)
            {
                Game.DisableTextInput();
            }
        }

        public void Draw(TimeSpan delta)
        {
            Game.RenderState.DepthEnabled = false;
            foreach (var scn in Scenes.Reverse())
            {
                foreach (var elem in scn.Elements)
                    elem.Draw(delta);
            }
        }

        public void Dispose()
        {
            Game.Mouse.MouseDown -= Mouse_MouseDown;
            Game.Mouse.MouseUp -= Mouse_MouseUp;
            Game.Keyboard.KeyUp -= Keyboard_KeyDown;
            Game.Keyboard.TextInput -= Keyboard_TextInput;
            foreach (var scene in Scenes) scene.Dispose();
            foreach (var v in sounds.Values)
                v.Dispose();
        }
    }
}
