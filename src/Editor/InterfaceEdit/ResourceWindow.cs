// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Numerics;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Interface;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using ImGuiNET;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;

namespace InterfaceEdit
{
    public partial class ResourceWindow
    {
        InterfaceResources resources;
        private UiData context;
        private MainWindow mainWindow;
        TextBuffer colorName = new TextBuffer();
        TextBuffer modelName = new TextBuffer();

        public ResourceWindow(MainWindow mainWindow, UiData context)
        {
            this.resources = context.Resources;
            this.context = context;
            this.mainWindow = mainWindow;
        }

        public bool IsOpen = false;
        public bool Draw()
        {
            if (IsOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(550,350), ImGuiCond.FirstUseEver);
                ImGui.Begin("Resources", ref IsOpen);
                if (ImGui.Button("Save")) {
                   var path = context.FileSystem.GetBackingFileName(Path.Combine(context.XInterfacePath, "resources.xml"));
                   File.WriteAllText(path, resources.ToXml());
                }
                ImGui.BeginTabBar("##tabbar", ImGuiTabBarFlags.None);
                if (ImGui.BeginTabItem("Colors"))
                {
                    ColorTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Models"))
                {
                    ModelTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Images"))
                {
                    ImageTab();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Library Files"))
                {
                    LibraryFilesTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            }

            return IsOpen;
        }

        private int _selColIndex = -1;
        public void ColorTab()
        {
            ImGui.BeginChild("##tabinner");
            if (ImGui.Button("Add"))
            {
                resources.Colors.Add(new InterfaceColor() {Name = "Color" + resources.Colors.Count, Color = Color4.White});
            }
            ImGui.Separator();
            ImGui.Columns(2);
            ImGui.BeginChild("##items");
            for (int i = 0; i < resources.Colors.Count; i++)
            {
                if (ImGui.Selectable(ImGuiExt.IDWithExtra(resources.Colors[i].Name, i.ToString()), _selColIndex == i))
                {
                    _selColIndex = i;
                    colorName.SetText(resources.Colors[i].Name);
                }
            }
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##pane");
            if (_selColIndex >= 0 && _selColIndex < resources.Colors.Count)
            {
                var clr = resources.Colors[_selColIndex];
                ImGui.SameLine(ImGui.GetColumnWidth() - 65);
                if (ImGui.Button("Delete"))
                {
                    resources.Colors.RemoveAt(_selColIndex);
                    _selColIndex = -1;
                }

                ImGui.Separator();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name: ");
                ImGui.SameLine();
                colorName.InputText("##Name", ImGuiInputTextFlags.None);
                clr.Name = colorName.GetText();
                if (clr.Animation == null)
                {

                    ColorPickerSimple(clr);
                    if (ImGui.Button("Make Animated"))
                    {
                        clr.Animation = new InterfaceColorAnimation()
                        {
                            Color1 = clr.Color
                        };
                    }
                }
                else
                {
                    ColorPickerAnimated(clr);
                    if (ImGui.Button("Make Simple"))
                    {
                        clr.Color = clr.Animation.Color1;
                        clr.Animation = null;
                    }
                }

            }
            ImGui.EndChild();
            ImGui.EndChild();

        }

        void ColorPickerSimple(InterfaceColor clr)
        {
            var v4 = (Vector4) clr.Color;
            ImGui.BeginChild("##limiter", new Vector2(250, 235));
            ImGui.ColorPicker4("##colorpicker", ref v4, ImGuiColorEditFlags.None);
            ImGui.EndChild();
            clr.Color = new Color4(v4.X, v4.Y, v4.Z, v4.W);
        }
        void ColorPickerAnimated(InterfaceColor clr)
        {
            var current = clr.GetColor(mainWindow.TotalTime);
            ImGui.ColorButton("##preview", current);
            ImGui.Text("Speed: ");
            ImGui.SameLine();
            ImGui.InputFloat("##speed", ref clr.Animation.Speed, 0, 0);
            ImGui.Text("Color 1");
            var v4 = (Vector4) clr.Animation.Color1;
            ImGui.BeginChild("##limiter", new Vector2(250, 235));
            ImGui.ColorPicker4("##colorpicker", ref v4, ImGuiColorEditFlags.None);
            ImGui.EndChild();
            clr.Animation.Color1 = v4;
            v4 = clr.Animation.Color2;
            ImGui.Text("Color 2");
            ImGui.BeginChild("##limiter2", new Vector2(250, 235));
            ImGui.ColorPicker4("##colorpicker", ref v4, ImGuiColorEditFlags.None);
            ImGui.EndChild();
            clr.Animation.Color2 = v4;
        }

        private static Func<string, bool> resourceFilter =
            VfsFileSelector.MakeFilter(".utf", ".vms", ".mat", ".txm", ".3db", ".cmp");

        private static Func<string, bool> modelFilter =
            VfsFileSelector.MakeFilter(".3db", ".cmp");
        public void LibraryFilesTab()
        {
            ImGui.BeginChild("##libtabinner");
            {
                if (ImGui.Button("Add"))
                {
                    mainWindow.Popups.OpenPopup(new VfsFileSelector("Add",
                        mainWindow.Project.UiData.FileSystem,
                        mainWindow.Project.UiData.DataPath,
                        newfile =>
                        {
                            resources.LibraryFiles.Add(newfile);
                            context.LoadLibraries();
                        }, resourceFilter));
                }

                ImGui.Separator();
                ImGui.BeginChild("##items");
                foreach (var file in resources.LibraryFiles)
                    ImGui.Selectable(file);
                ImGui.EndChild();
            }
            ImGui.EndChild();
        }

        private int _selModelIndex = -1;
        private RigidModel drawable;
        public void ModelTab()
        {
            ImGui.BeginChild("##tabinner");
            if (ImGui.Button("Add"))
            {
                mainWindow.Popups.OpenPopup(new VfsFileSelector("Add",
                    mainWindow.Project.UiData.FileSystem,
                    mainWindow.Project.UiData.DataPath,
                    newfile =>
                    {
                        var name = Path.GetFileName(newfile);
                        resources.Models.Add(new InterfaceModel()
                        {
                            Name = name, Path = newfile
                        });
                    }, modelFilter));
            }
            ImGui.Columns(2);
            ImGui.BeginChild("##items");
            for (int i = 0; i < resources.Models.Count; i++)
            {
                if (ImGui.Selectable(ImGuiExt.IDWithExtra(resources.Models[i].Name, i.ToString()), _selModelIndex == i))
                {
                    _selModelIndex = i;
                    drawable = context.GetModel(resources.Models[i].Path);
                    modelName.SetText(resources.Models[i].Name);
                }
            }
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##pane");
            if (_selModelIndex >= 0 && _selModelIndex < resources.Models.Count)
            {
                var mdl = resources.Models[_selModelIndex];
                ImGui.SameLine(ImGui.GetColumnWidth() - 65);
                if (ImGui.Button("Delete"))
                {
                    resources.Models.RemoveAt(_selModelIndex);
                    _selModelIndex = -1;
                }
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name: ");
                ImGui.SameLine();
                modelName.InputText("##Name", ImGuiInputTextFlags.None);
                mdl.Name = modelName.GetText();
                ImGui.Text($"Path: {mdl.Path}");
                ImGui.InputFloat("Offset X", ref mdl.X);
                ImGui.InputFloat("Offset Y", ref mdl.Y);
                ImGui.InputFloat("Scale X", ref mdl.XScale);
                ImGui.InputFloat("Scale Y", ref mdl.YScale);
                ImGui.Checkbox("XZ Plane", ref mdl.XZPlane);
                DoViewport(mdl);
            }

            ImGui.EndChild();
            ImGui.EndChild();
        }

        private Texture2D foundTexture;
        private int foundTextureId;
        private int _selTexIndex = -1;
        private TextBuffer imageName = new TextBuffer(256);
        private TextBuffer imageId = new TextBuffer(256);
        public void ImageTab()
        {
            ImGui.BeginChild("##tabinner");
            if (ImGui.Button("Add Normal"))
            {
                resources.Images.Add(new InterfaceImage() {Name = "Image" + resources.Images.Count});
            }
            ImGui.SameLine();
            if (ImGui.Button("Add Triangle"))
            {
                resources.Images.Add(new InterfaceImage() { Name = "Image" + resources.Images.Count, Type = InterfaceImageKind.Triangle });
            }
            ImGui.Separator();
            ImGui.Columns(2);
            ImGui.BeginChild("##items");
            for (int i = 0; i < resources.Images.Count; i++)
            {
                if (ImGui.Selectable(ImGuiExt.IDWithExtra(resources.Images[i].Name, i.ToString()), _selTexIndex == i))
                {
                    _selTexIndex = i;
                    imageName.SetText(resources.Images[i].Name);
                    imageId.SetText(resources.Images[i].TexName ?? "");
                }
            }
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##pane");
            if (_selTexIndex >= 0 && _selTexIndex < resources.Images.Count)
            {
                var img = resources.Images[_selTexIndex];
                ImGui.SameLine(ImGui.GetColumnWidth() - 65);
                if (ImGui.Button("Delete"))
                {
                    resources.Images.RemoveAt(_selTexIndex);
                    _selTexIndex = -1;
                }
                ImGui.Separator();
                var ft2 = context.ResourceManager.FindTexture(img.TexName) as Texture2D;
                if (foundTexture != ft2)
                {
                    if(foundTexture != null) ImGuiHelper.DeregisterTexture(foundTexture);
                    foundTexture = ft2;
                    if(ft2 != null) foundTextureId = ImGuiHelper.RegisterTexture(foundTexture);
                }
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name: ");
                ImGui.SameLine();
                imageName.InputText("##Name", ImGuiInputTextFlags.None);
                img.Name = imageName.GetText();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Texture: ");
                ImGui.SameLine();
                imageId.InputText("##Texture", ImGuiInputTextFlags.None);
                img.TexName = imageId.GetText();
                if (img.Type != InterfaceImageKind.Triangle)
                {
                    bool flip = img.Flip;
                    ImGui.Checkbox("Flip", ref flip);
                    img.Flip = flip;
                }

                //DRAW. No controls below here
                DoImagePreview(img);
            }

            ImGui.EndChild();
            ImGui.EndChild();
        }

        void BindViewport(int szX, int szY)
        {
            if (szY <= 0) szY = 1;
            if (rtX != szX || rtY != szY)
            {
                rtX = szX;
                rtY = szY;
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget.Texture);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(mainWindow.RenderContext, rtX, rtY);
                renderTargetImage = ImGuiHelper.RegisterTexture(renderTarget.Texture);
            }
            mainWindow.RenderContext.RenderTarget = renderTarget;
            mainWindow.RenderContext.PushViewport(0,0,rtX,rtY);
            mainWindow.RenderContext.ClearColor = Color4.Black;
            mainWindow.RenderContext.ClearAll();
        }

        void DrawViewport()
        {
            mainWindow.RenderContext.PopViewport();
            mainWindow.RenderContext.RenderTarget = null;
            var cPos = ImGui.GetCursorPos();
            ImGui.Image((IntPtr) renderTargetImage, new Vector2(rtX, rtY), new Vector2(0, 1), new Vector2(1, 0));
            ImGui.SetCursorPos(cPos);
            ImGui.InvisibleButton("##renderThing", new Vector2(rtX, rtY));
        }

        private RenderTarget2D renderTarget;
        private int renderTargetImage;
        private int rtX = -1, rtY = -1;
        void DoViewport(InterfaceModel mdl)
        {
            if (drawable == null) return;
            var szX = (int) ImGui.GetColumnWidth() - 5;
            var szY = (int) ImGui.GetWindowContentRegionMax().Y - (int) ImGui.GetCursorPosY() - 5;
            BindViewport(szX, szY);
            //Do drawing
            var rectangle = new Rectangle(5, 5, rtX - 10, rtY - 10);
            mainWindow.RenderContext.Renderer2D.FillRectangle(rectangle, Color4.CornflowerBlue);

            var scale = mdl.XZPlane
                ? new Vector3(mdl.XScale, 1, mdl.YScale)
                : new Vector3(mdl.XScale, mdl.YScale, 1);
            var rotation = mdl.XZPlane
                ? Matrix4x4.CreateRotationX(MathF.PI / 2f)
                : Matrix4x4.Identity;
            var transform = Matrix4x4.CreateScale(scale) *
                            rotation *
                            Matrix4x4.CreateTranslation(mdl.X, mdl.Y, 0);
            transform *= DisplayModel.CreateTransform(rtX, rtY, rectangle);
            mainWindow.RenderContext.SetIdentityCamera();
            drawable.Update(mainWindow.TotalTime);
            mainWindow.RenderContext.Cull = false;
            mainWindow.RenderContext.ScissorEnabled = true;
            mainWindow.RenderContext.ScissorRectangle = rectangle;
            drawable.DrawImmediate(mainWindow.RenderContext, context.ResourceManager, transform, ref Lighting.Empty);
            mainWindow.RenderContext.ScissorEnabled = false;
            mainWindow.RenderContext.Cull = true;
            DrawViewport();
        }

        void DoImagePreview(InterfaceImage mdl)
        {
            if (foundTexture == null)
            {
                ImGui.TextColored(Color4.Red, "Texture not found");
                return;
            }

            if (mdl.Type == InterfaceImageKind.Triangle)
            {
                EditTriangle(mdl);
            }
            else
            {
                if (ImGui.Button("0°")) mdl.Rotation = QuadRotation.None;
                ImGui.SameLine();
                if (ImGui.Button("90°")) mdl.Rotation = QuadRotation.Rotate90;
                ImGui.SameLine();
                if (ImGui.Button("180°")) mdl.Rotation = QuadRotation.Rotate180;
                ImGui.SameLine();
                if (ImGui.Button("270°")) mdl.Rotation = QuadRotation.Rotate270;
                ImGui.Text($"Rotation: {(int)mdl.Rotation * 90}°");
                var szX = (int) ImGui.GetColumnWidth() - 5;
                szX = Math.Min(szX, 150);
                var ratio = foundTexture.Height / (float)foundTexture.Width;
                var szY = (int) (szX * ratio);
                //source
                ImGui.Text("Source:");
                ImGui.SliderFloat("X", ref mdl.TexCoords.X0, 0, 1);
                ImGui.SliderFloat("Y", ref mdl.TexCoords.Y0, 0, 1);
                ImGui.SliderFloat("Width", ref mdl.TexCoords.X3, 0, 1);
                ImGui.SliderFloat("Height", ref mdl.TexCoords.Y3, 0, 1);
                float angle = MathHelper.RadiansToDegrees(mdl.Angle);
                ImGui.SliderFloat("Angle", ref angle, 0, 360);
                ImGui.SliderFloat("Origin X", ref mdl.OriginX, 0, 1);
                ImGui.SliderFloat("Origin Y", ref mdl.OriginY, 0, 1);
                ImGui.SliderFloat("Animate U", ref mdl.AnimU, 0, 10);
                ImGui.SliderFloat("Animate V", ref mdl.AnimV, 0, 10);
                mdl.Angle = MathHelper.DegreesToRadians(angle);

                var aX = (float)mainWindow.TotalTime * (mdl.AnimU * (mdl.TexCoords.X3 - mdl.TexCoords.X0));
                var aY = (float)mainWindow.TotalTime * (mdl.AnimV * (mdl.TexCoords.Y3 - mdl.TexCoords.Y0));
                var anim = new Vector2(aX, aY);
                //res
                var x0 = mdl.TexCoords.X0;
                var x1 = mdl.TexCoords.X0 + mdl.TexCoords.X3;
                var y0 = mdl.TexCoords.Y0;
                var y1 = mdl.TexCoords.Y0 + mdl.TexCoords.Y3;
                var a = new Vector2(x0, y0) + anim;
                var b = new Vector2(x1, y0) + anim;
                var c = new Vector2(x0, y1) + anim;
                var d = new Vector2(x1, y1) + anim;
                if (mdl.Flip)
                {
                    a.Y = b.Y = y1;
                    c.Y = d.Y = y0;
                }
                Vector2 tl = a, tr = b, bl = c, br = d;
                if (mdl.Rotation == QuadRotation.Rotate90)
                {
                    tl = c;
                    tr = a;
                    bl = d;
                    br = b;
                }
                else if (mdl.Rotation == QuadRotation.Rotate180)
                {
                    tl = d;
                    tr = c;
                    bl = b;
                    br = a;
                }
                else if (mdl.Rotation == QuadRotation.Rotate270)
                {
                    tl = b;
                    tr = d;
                    bl = a;
                    br = c;
                }

                //pos
                var cPos = (Vector2)ImGui.GetCursorPos();
                var wPos = (Vector2)ImGui.GetWindowPos();
                var scrPos = -ImGui.GetScrollY();
                var xy = cPos + wPos + new Vector2(0, scrPos);
                var sz = new Vector2(szX, szY);

                var ogX = mdl.OriginX * szX;
                var ogY = mdl.OriginY * szY;
                var (p1, p2, p3, p4) = GetQuad(ogX + xy.X, ogY + xy.Y, szX, szY,
                    ogX, ogY, mdl.Angle);

                ImGui.GetWindowDrawList().AddImageQuad(
                    (IntPtr)foundTextureId,
                    p1,p2,p3,p4,
                    tl,tr,br,bl,
                    UInt32.MaxValue
                    );

                var originPos = xy + (sz * new Vector2(mdl.OriginX, mdl.OriginY));
                if (originPos != Vector2.Zero)
                {
                    ImGui.GetWindowDrawList().AddCircleFilled(originPos, 4, 0xFFFFFFFF);
                    ImGui.GetWindowDrawList().AddCircleFilled(originPos, 2, 0xFF0000FF);
                }

                ImGui.Dummy(new Vector2(szX, szY));
            }

        }

        (Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) GetQuad(float x, float y, float w, float h, float originX, float originY, float angle)
        {
            float dx = -originX;
            float dy = -originY;

            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            var tl = new Vector2(
                x + dx * cos - dy * sin,
                y + dx * sin + dy * cos
            );
            var tr = new Vector2(
                x+(dx+w)*cos-dy*sin,
                y+(dx+w)*sin+dy*cos
            );
            var bl = new Vector2(
                x + dx * cos - (dy + h) * sin,
                y + dx * sin + (dy + h) * cos
            );
            var br = new Vector2(
                x+(dx+w)*cos-(dy+h)*sin,
                y+(dx+w)*sin+(dy+h)*cos
            );
            return (tl, tr, br, bl);
        }

        public void Dispose()
        {
            colorName.Dispose();
            modelName.Dispose();
        }
    }
}
