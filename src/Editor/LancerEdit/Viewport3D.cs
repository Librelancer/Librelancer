// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Graphics;

namespace LancerEdit
{

    public delegate void ViewportDraw(int width, int height);

    public class Viewport3D : IDisposable
    {
        RenderContext rstate;
        int rtWidth = -1, rtHeight = -1;
        int mrw = -1, mrh = -1, msamples = 0;
        public RenderTarget2D RenderTarget;
        MultisampleTarget msaa;
        public Vector3 DefaultOffset = new Vector3(0, 0, 200);

        public float ModelScale = 0.25f;
        public Vector2 ModelRotation = Vector2.Zero;
        public Vector2 CameraRotation = Vector2.Zero;
        public float MarginH = 0;
        public float MarginW = 0;
        public int MinWidth = 120;
        public int MinHeight = 120;

        public Vector3 CameraOffset = Vector3.Zero;
        public Color4 Background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        public CameraModes Mode = CameraModes.Arcball;
        public bool EnableMSAA = true;
        public bool ClearArea = true;

        MainWindow mw;
        public Viewport3D(MainWindow mw)
        {
            this.mw = mw;
            rstate = mw.RenderContext;
        }

        private float zoom;
        private Vector2 orbitPan;
        public void ResetControls()
        {
            CameraOffset = DefaultOffset;
            zoom = DefaultOffset.Z;
            orbitPan = Vector2.Zero;
            ModelRotation = CameraRotation = Vector2.Zero;
        }
        Color4 cc;

        public ViewportDraw Draw3D;

        public int ControlWidth { get; private set; } = 100;
        public int ControlHeight { get; private set; } = 100;

        public void DrawRenderTarget(int fixWidth, int fixHeight)
        {
            //Generate render target
            if (rtHeight != fixHeight || rtWidth != fixWidth)
            {
                if (RenderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(RenderTarget.Texture);
                    RenderTarget.Dispose();
                }
                RenderTarget = new RenderTarget2D(rstate, fixWidth, fixHeight);
                rtWidth = fixWidth;
                rtHeight = fixHeight;
            }

            bool useMSAA = CheckMSAA(fixWidth, fixHeight);
            rstate.RenderTarget = useMSAA ? msaa : RenderTarget;
            rstate.PushViewport(0, 0, fixWidth, fixHeight);
            rstate.Cull = true;
            rstate.DepthEnabled = true;
            rstate.ClearColor = Background;
            rstate.ClearAll();
            Draw3D(fixWidth, fixHeight);
            rstate.PopViewport();
            rstate.RenderTarget = null;
            if (useMSAA)
                msaa.BlitToRenderTarget(RenderTarget);
            rstate.ClearColor = cc;
            rstate.DepthEnabled = false;
            rstate.BlendMode = BlendMode.Normal;
            rstate.Cull = false;
        }

        bool CheckMSAA(int width, int height)
        {
            bool msaaEnabled = EnableMSAA && mw.Config.MSAA != 0;
            if (msaaEnabled && ((mrw != width) || (mrh != height) || (msamples != mw.Config.MSAA)))
            {
                if (msaa != null) msaa.Dispose();
                msaa = new MultisampleTarget(rstate, width, height, mw.Config.MSAA);
                mrw = width;
                mrh = height;
            }
            if(!msaaEnabled && msaa != null)
            {
                msaa.Dispose();
                mrw = mrh = -1;
                msamples = 0;
                msaa = null;
            }
            return msaaEnabled;
        }

        public unsafe void Draw(int fixWidth = -1, int fixHeight = -1, bool view = true)
        {
            if (mw.Width <= 0 || mw.Height <= 0)
                return;
            ImGuiHelper.AnimatingElement();
            var avail = ImGui.GetContentRegionAvail();
            var renderWidth = Math.Max(MinWidth, (int)(avail.X - MarginW));
            var renderHeight = Math.Max(MinHeight, (int)(avail.Y - MarginH));
            if (fixWidth > 0) renderWidth = fixWidth;
            if (fixHeight > 0) renderHeight = fixHeight;
            ControlWidth = renderWidth;
            ControlHeight = renderHeight;
            if (view)
            {
                var cpos = ImGui.GetCursorScreenPos();
                MousePos = ImGui.GetMousePos() - cpos;
                ImGuizmo.SetRect(cpos.X, cpos.Y, ControlWidth, ControlHeight);
                ImGuizmo.SetDrawlist();
                ImGui.GetWindowDrawList().AddCallback((_, _) =>
                {
                    DrawCallback((int)cpos.X, (int)cpos.Y, renderWidth, renderHeight);
                }, IntPtr.Zero);
                ImGui.GetWindowDrawList().AddRect(cpos, cpos + new Vector2(renderWidth, renderHeight), ImGui.GetColorU32(ImGuiCol.Border));
                bool click = false;
                //Taken from imgui_internal.h
                const int MouseButtonLeft = 1 << 0;
                const int PressedOnClickRelease = 1 << 5;
                const int PressedOnDoubleClick = 1 << 8;
                const ImGuiButtonFlags Flags =
                    (ImGuiButtonFlags) (MouseButtonLeft | PressedOnClickRelease | PressedOnDoubleClick);
                ImGui.SetNextItemAllowOverlap();
                if (inputsEnabled)
                    click = ImGui.InvisibleButton("##button", new Vector2(ControlWidth, ControlHeight), Flags);
                else
                    ImGui.Dummy(new Vector2(renderWidth, renderHeight));
                if (Mode == CameraModes.Cockpit) ModelRotation = Vector2.Zero;
                if (Mode == CameraModes.Arcball) ArcballUpdate();
                if (click && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    MouseInFrame = true;
                    DoubleClicked?.Invoke(MousePos);
                }
                else if (inputsEnabled && ImGui.IsItemHovered(ImGuiHoveredFlags.None))
                {
                    MouseInFrame = true;
                    switch(Mode)
                    {
                        case CameraModes.Walkthrough:
                            WalkthroughControls();
                            break;
                        case CameraModes.Starsphere:
                            StarsphereControls();
                            break;
                        case CameraModes.Arcball:
                            ArcballControls();
                            break;
                    }
                }

            }
        }

        void DrawCallback(int x, int y, int w, int h)
        {
            cc = rstate.ClearColor;
            var useMsaa = CheckMSAA(w, h);
            if (useMsaa)
            {
                rstate.RenderTarget = msaa;
                rstate.PushViewport(0, 0, w, h);
                rstate.PushScissor(new Rectangle(0, 0, w, h), false);
            }
            else
            {
                rstate.PushViewport(x, y, w, h);
                rstate.PushScissor(new Rectangle(x, y, w, h), false);
            }
            rstate.Cull = true;
            rstate.DepthEnabled = true;
            rstate.ClearColor = Background;
            if (useMsaa)
            {
                rstate.ClearAll();
            }
            else if (ClearArea)
            {
                rstate.ClearColorOnly();
            }
            Draw3D(w, h);
            rstate.PopViewport();
            rstate.ClearColor = cc;
            rstate.DepthEnabled = false;
            rstate.BlendMode = BlendMode.Normal;
            rstate.Cull = false;
            if (useMsaa)
            {
                rstate.PopScissor();
                rstate.RenderTarget = null;
                msaa.BlitToScreen(new Point(x,y));
            }
            else
            {
                rstate.PopScissor();
            }

        }

        private bool inputsEnabled = true;
        public void SetInputsEnabled(bool enabled)
        {
            inputsEnabled = enabled;
        }

        public event Action<Vector2> DoubleClicked;

        struct SavedControls
        {
            public CameraModes Mode;
            public Vector2 OrbitPan;
            public Vector2 ModelRotation;
            public Vector2 CameraRotation;
            public Vector3 CameraOffset;
            public float Zoom;
        }

        public string ExportControls()
        {
            Span<SavedControls> save = stackalloc SavedControls[1];
            save[0].Mode = Mode;
            save[0].OrbitPan = orbitPan;
            save[0].CameraRotation = CameraRotation;
            save[0].CameraOffset = CameraOffset / ModelScale;
            save[0].Zoom = zoom / ModelScale;
            save[0].ModelRotation = ModelRotation;
            Span<byte> bytes = MemoryMarshal.Cast<SavedControls, byte>(save);
            return Convert.ToBase64String(bytes).Replace('=','_');
        }

        public void ImportControls(string preset)
        {
            var bytes = Convert.FromBase64String(preset.Replace('_', '='));
            var save = MemoryMarshal.Cast<byte, SavedControls>(bytes.AsSpan());
            ResetControls();
            Mode = save[0].Mode;
            orbitPan = save[0].OrbitPan;
            CameraRotation = save[0].CameraRotation;
            CameraOffset = ModelScale * save[0].CameraOffset;
            zoom = ModelScale * save[0].Zoom;
            ModelRotation = save[0].ModelRotation;
            if(Mode == CameraModes.Arcball)
                ArcballUpdate();
        }

        public bool MouseInFrame;
        public Vector2 MousePos;

        float GotoRadius => ModelScale * 5.2f;

        public void GoTop()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
            orbitPan = new Vector2(0, (float) -Math.PI + 0.03f);
            ArcballUpdate();
            if (Mode == CameraModes.Walkthrough)
                CameraRotation = new Vector2(0, (float) (-0.5 * Math.PI));
        }

        public void GoBottom()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
            orbitPan = new Vector2(0,  (float)Math.PI - 0.03f);
            ArcballUpdate();
            if(Mode == CameraModes.Walkthrough)
                CameraRotation = new Vector2(0, (float)(0.5 * Math.PI));
        }

        public void GoLeft()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
            orbitPan = new Vector2((float) (0.5 * Math.PI), 0);
            ArcballUpdate();
            if(Mode == CameraModes.Walkthrough)
                CameraRotation = new Vector2((float) (-0.5f * Math.PI), 0);
        }

        public void GoRight()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
            orbitPan = new Vector2((float) (-0.5 * Math.PI), 0);
            ArcballUpdate();
            if(Mode == CameraModes.Walkthrough)
                CameraRotation = new Vector2((float) (0.5f * Math.PI), 0);
        }

        public void GoFront()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
            orbitPan = new Vector2((float)(Math.PI), 0);
            ArcballUpdate();
            if(Mode == CameraModes.Walkthrough)
                CameraRotation = new Vector2((float) (-Math.PI), 0);
        }

        public void GoBack()
        {
            if (Mode == CameraModes.Starsphere || Mode == CameraModes.Cockpit) return;
            ResetControls();
        }

        void ArcballUpdate()
        {
            orbitPan.Y = MathHelper.Clamp(orbitPan.Y,-MathHelper.PiOver2 + 0.02f, MathHelper.PiOver2 - 0.02f);
            var mat = System.Numerics.Matrix4x4.CreateFromYawPitchRoll(-orbitPan.X, orbitPan.Y, 0);
            var res = Vector3.Transform(new Vector3(0, 0, zoom), mat);
            CameraRotation = Vector2.Zero;
            CameraOffset = new Vector3(res.X, res.Y, res.Z);
        }
        void ArcballControls()
        {
            var io = ImGui.GetIO();
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left, 1f))
            {
                var delta = (Vector2)ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 1f);
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
                if (io.KeyCtrl)
                    ModelRotation += (delta / 100) * new Vector2(1, -1);
                else
                    orbitPan += (delta / 100) * new Vector2(1, -1);
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Right, 1f))
            {
                var delta = (Vector2)ImGui.GetMouseDragDelta(ImGuiMouseButton.Right, 1f);
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Right);
                var mouseZoomStep = ModelScale / 56f;
                zoom -= delta.Y * mouseZoomStep;
            }

            float wheel = ImGui.GetIO().MouseWheel;
            var zoomStep = ModelScale * 1.05f;
            if (io.KeyShift)
                zoom -= wheel * (2 * zoomStep);
            else
                zoom -= wheel * zoomStep;
            if (zoom < 0) zoom = 0;
        }

        void StarsphereControls()
        {
            //Only rotate camera
            if (ImGui.IsMouseDragging(0, 1f))
            {
                var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
                ImGui.ResetMouseDragDelta(0);
                //LMB - Rotate viewport camera
                CameraRotation -= (delta / 100);
            }
        }

        void WalkthroughControls()
        {
            var io = ImGui.GetIO();
            if (ImGui.IsMouseDragging(0, 1f))
            {
                var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
                ImGui.ResetMouseDragDelta(0);
                var rotmat = Matrix4x4.CreateRotationX(CameraRotation.Y) *
                    Matrix4x4.CreateRotationY(CameraRotation.X);
                if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                {
                    //LMB + RMB - Move up and down
                    ImGui.ResetMouseDragDelta(ImGuiMouseButton.Right);
                    var y = Vector3.Transform(Vector3.UnitY, rotmat);
                    CameraOffset += y * (delta.Y * ModelScale / 52f);
                }
                else
                {
                    var z = Vector3.Transform(Vector3.UnitZ,rotmat);
                    var x = Vector3.Transform(Vector3.UnitX, rotmat);

                    CameraOffset += x * (delta.X * ModelScale / 52f);
                    CameraOffset -= z * (delta.Y * ModelScale / 44f);
                }
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Right, 1f))
            {
                var delta = (Vector2)ImGui.GetMouseDragDelta(ImGuiMouseButton.Right, 1f);
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Right);
                if (io.KeyCtrl)
                {
                    //CTRL + RMB - Rotate Model
                    ModelRotation += (delta / 100) * new Vector2(1, -1);
                }
                else
                {
                    //RMB - Rotate viewport camera
                    CameraRotation -= (delta / 100);
                    WalkthroughKeyboardControls();
                }
            }
            else if (!io.WantCaptureKeyboard)
                WalkthroughKeyboardControls();
        }
        void WalkthroughKeyboardControls()
        {
            var rotmat = Matrix4x4.CreateRotationX(CameraRotation.Y) *
                            Matrix4x4.CreateRotationY(CameraRotation.X);
            if (mw.Keyboard.IsKeyDown(Keys.W))
            {
                var z = Vector3.Transform(-Vector3.UnitZ, rotmat);
                CameraOffset += z * (float)mw.TimeStep * ModelScale;
            }
            else if (mw.Keyboard.IsKeyDown(Keys.S))
            {
                var z = Vector3.Transform(Vector3.UnitZ, rotmat);
                CameraOffset += z * (float)mw.TimeStep * ModelScale;
            }
            if (mw.Keyboard.IsKeyDown(Keys.A))
            {
                var x = Vector3.Transform(-Vector3.UnitX, rotmat);
                CameraOffset += x * (float)mw.TimeStep * ModelScale;
            }
            else if (mw.Keyboard.IsKeyDown(Keys.D))
            {
                var x = Vector3.Transform(Vector3.UnitX, rotmat);
                CameraOffset += x * (float)mw.TimeStep * ModelScale;
            }
            if (mw.Keyboard.IsKeyDown(Keys.E))
            {
                var y = Vector3.Transform(Vector3.UnitY, rotmat);
                CameraOffset += y * (float)mw.TimeStep * ModelScale;
            }
            else if (mw.Keyboard.IsKeyDown(Keys.Q))
            {
                var y = Vector3.Transform(-Vector3.UnitY, rotmat);
                CameraOffset += y * (float)mw.TimeStep * ModelScale;
            }
        }
        public void Dispose()
        {
            msaa?.Dispose();
            RenderTarget?.Dispose();
        }
    }
}
