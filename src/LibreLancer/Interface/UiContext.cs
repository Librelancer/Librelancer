// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer;
using LibreLancer.Data;

namespace LibreLancer.Interface
{
    public class UiContext
    {
        //State
        public float ViewportWidth;
        public float ViewportHeight;
        public float MouseX;
        public float MouseY;
        public bool MouseLeftDown;
        public TimeSpan GlobalTime;
        //Rendering
        public RenderState RenderState;
        public Renderer2D Renderer2D;
        public MatrixCamera MatrixCam = new MatrixCamera(Matrix4.Identity);
        //Data
        public string DataPath;
        public GameResourceManager ResourceManager;
        public InfocardManager Infocards;
        public FontManager Fonts;
        public FileSystem FileSystem;
        public Dictionary<string,string> NavbarIcons;
        public SoundManager Sounds;
        //Ui
        public Stylesheet Stylesheet;
        public UiXmlLoader XmlLoader;
        public InterfaceResources Resources;
        public string XInterfacePath;
        public object GameApi;
        //Editor-only
        public string FlDirectory;
        //State
        private bool mode2d = false;
        private FreelancerGame game;
        public UiContext()
        {
        }

        public string GetNavbarIconPath(string icon)
        {
            var p = DataPath.Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(p, NavbarIcons[icon]);
        }
        public string GetFont(string fontName)
        {
            if (fontName[0] == '$') fontName = Fonts.ResolveNickname(fontName.Substring(1));
            return fontName;
        }

        public InterfaceColor GetColor(string color)
        {
            var clr = Resources.Colors.FirstOrDefault(x => x.Name.Equals(color, StringComparison.OrdinalIgnoreCase));
            return clr ?? new InterfaceColor() {
                Color = Parser.Color(color)
            };
        }
        
        public UiContext(FreelancerGame game)
        {
            Renderer2D = game.Renderer2D;
            RenderState = game.RenderState;
            ResourceManager = game.ResourceManager;
            FileSystem = game.GameData.VFS;
            Infocards = game.GameData.Ini.Infocards;
            Fonts = game.Fonts;
            NavbarIcons = game.GameData.GetBaseNavbarIcons();
            Sounds = game.Sound;
            DataPath = game.GameData.Ini.Freelancer.DataPath;
            if (!string.IsNullOrWhiteSpace(game.GameData.Ini.Freelancer.XInterfacePath))
                OpenFolder(game.GameData.Ini.Freelancer.XInterfacePath);
            else
                OpenDefault();
            this.game = game;
            game.Mouse.MouseDown += MouseOnMouseDown;
            game.Mouse.MouseUp += MouseOnMouseUp;
        }

        private void MouseOnMouseUp(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseClick();
                OnMouseUp();
            }
        }

        private void MouseOnMouseDown(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left) OnMouseDown();
        }

        public void OpenFolder(string xinterfacePath)
        {
            XInterfacePath = xinterfacePath;
            ReadResourcesAndStylesheet();
        }

        Dictionary<string, Texture2D> loadedFiles = new Dictionary<string,Texture2D>();
        public Texture2D GetTextureFile(string filename)
        {
            try
            {
                var file = FileSystem.Resolve(filename);
                if (!loadedFiles.ContainsKey(file))
                {
                    loadedFiles.Add(file, LibreLancer.ImageLib.Generic.FromFile(file));
                }
                return loadedFiles[file];
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public void OpenDefault()
        {
            XInterfacePath = null;
            ReadResourcesAndStylesheet();
        }

        void ReadResourcesAndStylesheet()
        {
            Resources = InterfaceResources.FromXml(ReadAllText("resources.xml"));
            XmlLoader = new UiXmlLoader(Resources);
            Stylesheet = (Stylesheet) XmlLoader.FromString(ReadAllText("stylesheet.xml"), null);
            LoadLibraries();
        }
        
        public string ReadAllText(string file)
        {
            if (!string.IsNullOrEmpty(XInterfacePath))
            {
                var path = FileSystem.Resolve(Path.Combine(XInterfacePath, file));
                return File.ReadAllText(path);
            }
            else
            {
                using (var reader =
                    new StreamReader(
                        typeof(UiContext).Assembly.GetManifestResourceStream($"LibreLancer.Interface.Default.{file}")))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public UiWidget LoadXml(string file)
        {
            var widget = (UiWidget) XmlLoader.FromString(ReadAllText(file), null);
            widget.ApplyStylesheet(Stylesheet);
            return widget;
        }

        public void LoadLibraries()
        {
            foreach (var file in Resources.LibraryFiles)
            {
                ResourceManager.LoadResourceFile(FileSystem.Resolve(file));
            }
        }

        public RigidModel GetModel(string path)
        {
            if(string.IsNullOrEmpty(path)) return null;
            try
            {
                return ((IRigidModelFile) ResourceManager.GetDrawable(FileSystem.Resolve(path))).CreateRigidModel(true);
            }
            catch (Exception e)
            {
                FLLog.Error("UiContext",$"{e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        public Vector2 AnchorPosition(RectangleF parent, AnchorKind anchor, float x, float y, float width, float height)
        {
            float resolveX = 0;
            float resolveY = 0;
            switch (anchor)
            {
                case AnchorKind.TopLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.TopCenter:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.TopRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.CenterLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.Center:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.CenterRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.BottomLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
                case AnchorKind.BottomCenter:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
                case AnchorKind.BottomRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
            }
            return new Vector2(resolveX, resolveY);
        }
        
        public Vector2 PointsToPixels(Vector2 points)
        {
            var ratio = ViewportHeight / 480;
            return points * ratio;
        }

        public int PointsToPixels(float points)
        {
            var ratio = ViewportHeight / 480;
            var i = (int) (points * ratio);
            return i <= 0 ? 1 : i;
        }
        public Rectangle PointsToPixels(RectangleF points)
        {
            var ratio = ViewportHeight / 480;
            return new Rectangle((int)(points.X * ratio), (int)(points.Y * ratio), (int)(points.Width * ratio), (int)(points.Height * ratio));
        }

        public float GetScreenWidth()
        {
            var aspect = ViewportWidth / ViewportHeight;
            return 480 * aspect;
        }
        public float TextSize(float inputPoints)
        {
            var ratio = ViewportHeight / 480;
            var pixels = inputPoints * ratio;
            return (int)Math.Floor(pixels);
        }
        

        public void Mode2D()
        {
            if (mode2d) return;
            Renderer2D.Start((int)ViewportWidth, (int)ViewportHeight);
            mode2d = true;
        }
        
        public void Mode3D()
        {
            if (!mode2d) return;
            Renderer2D.Finish();
            mode2d = false;
        }
        
        public void Update(UiWidget widget, TimeSpan globalTime, int mouseX, int mouseY, bool leftDown)
        {
            GlobalTime = globalTime;
            var inputRatio = 480 / ViewportHeight;
            MouseX = mouseX * inputRatio;
            MouseY = mouseY * inputRatio;
            MouseLeftDown = leftDown;
        }

        public void Update(FreelancerGame game)
        {
            ViewportWidth = game.Width;
            ViewportHeight = game.Height;
            Update(baseWidget, TimeSpan.FromSeconds(game.TotalTime),
                game.Mouse.X, game.Mouse.Y, game.Mouse.IsButtonDown(MouseButtons.Left));
        }

        public void Unhook()
        {
            if (game != null)
            {
                game.Mouse.MouseUp -= MouseOnMouseUp;
                game.Mouse.MouseDown -= MouseOnMouseDown;
            }
        }
        public UiWidget CreateAll(string file)
        {
            var w = LoadXml(file);
            w.ApplyStylesheet(Stylesheet);
            w.EnableScripting(this, null);
            SetWidget(w);
            return w;
        }
        RectangleF GetRectangle() => new RectangleF(0,0, 480 * (ViewportWidth / ViewportHeight), 480);

        private UiWidget baseWidget;
        Stack<ModalState> modals = new Stack<ModalState>();
        private UiFullState fullState;
        public UiFullState SetWidget(UiWidget widget)
        {
            foreach (var m in modals)
                m.Widget.Dispose();
            modals = new Stack<ModalState>();
            baseWidget = widget;
            fullState = new UiFullState() {
                Widget = baseWidget,
                Modals = modals
            };
            return fullState;
        }

        public void SetFullState(UiFullState ctx)
        {
            modals = ctx.Modals;
            baseWidget = ctx.Widget;
            fullState = ctx;
        }

        public void OpenModal(string name, string modalData, Action<string> onClose)
        {
            var item = LoadXml(name);
            item.EnableScripting(this, modalData);
            modals.Push(new ModalState() {
                Widget = item, OnClose = onClose
            });
        }

        public void CloseModal(string data)
        {
            if (modals.Count > 0)
            {
                modals.Peek().OnClose?.Invoke(data);
                modals.Pop();
            }
        }

        public void PlaySound(string sound)
        {
            
        }
        
        UiWidget GetActive()
        {
            if (baseWidget == null) return null;
            if (modals.Count > 0) return modals.Peek().Widget;
            return baseWidget;
        }

        public void ChatboxEvent() =>   GetActive()?.ScriptedEvent("Chatbox");
        public void OnMouseDown() => GetActive()?.OnMouseDown(this, GetRectangle());
        public void OnMouseUp() => GetActive()?.OnMouseUp(this, GetRectangle());
        public void OnMouseClick() => GetActive()?.OnMouseClick(this, GetRectangle());

        private UiWidget textFocusWidget = null;

        internal void SetTextFocus(UiWidget widget) => textFocusWidget = widget;

        public bool KeyboardGrabbed => textFocusWidget != null;

        public void OnKeyDown(Keys key) => textFocusWidget?.OnKeyDown(key);

        public void OnTextEntry(string text) => textFocusWidget?.OnTextInput(text);
        public void RenderWidget()
        {
            if (baseWidget == null) return;
            textFocusWidget = null;
            RenderState.DepthEnabled = false;
            mode2d = false;
            var aspect = ViewportWidth / ViewportHeight;
            var desktopRect = new RectangleF(0, 0, 480 * aspect, 480);
            baseWidget.Render(this, desktopRect);
            foreach(var widget in modals.Reverse())
                widget.Widget.Render(this, desktopRect);
            if (mode2d)
                Renderer2D.Finish();
            RenderState.DepthEnabled = true;
        }
    }

    class ModalState
    {
        public UiWidget Widget;
        public Action<string> OnClose;
    }
    public class UiFullState
    {
        internal UiWidget Widget;
        internal Stack<ModalState> Modals;
    }
}