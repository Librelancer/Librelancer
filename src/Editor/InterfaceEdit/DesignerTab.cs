// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Linq;
using System.Xml;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Graphics;
using LibreLancer.Interface;
namespace InterfaceEdit
{
    public class DesignerTab : SaveableTab
    {
        private ColorTextEdit xmlEditor;
        private MainWindow mainWindow;
        private UiWidget widget;
        private UiContext context;
        private List<XmlObjectMap> objectMaps;
        private string savePath;

        public override string Filename => savePath;
        public DesignerTab(string text, string path, MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            Title = Path.GetFileName(path);
            savePath = path;
            xmlEditor = new ColorTextEdit();
            xmlEditor.SetText(text);
            context = new UiContext(mainWindow.Project.UiData);
            context.CommandBuffer = mainWindow.CommandBuffer;
            context.RenderContext = mainWindow.RenderContext;
            context.Lines = mainWindow.LineRenderer;
            context.GameApi = mainWindow.TestApi;
            TextChanged();
        }

        public override void Save()
        {
            File.WriteAllText(savePath, xmlEditor.GetText());
        }

        private float h1 = 200, h2 = 200;
        private bool propertiesOpen = false;
        private object editingObject = null;
        private XmlObjectMap editingMap = null;
        private bool validXml = false;
        private PropertyGrid propertyGrid = new PropertyGrid();
        //TODO: this method is really hacky and probably doesn't work reliably. Fill in gaps instead of weird reversing
        object FindEditingObject(int line, int column)
        {
            bool first = true;
            object obj = null;
            //Same line
            foreach (var map in objectMaps.Where(x => x.Line == line))
            {
                if (first) obj = map.Object;
                first = false;
                if (column >= map.Column)
                {
                    obj = map.Object;
                    editingMap = map;
                }
            }
            if (obj != null) return obj;
            first = true;
            //Otherwise find by reverse
            foreach (var map in ((IEnumerable<XmlObjectMap>)objectMaps).Reverse().Where(x => line >= x.Line))
            {
                if (first)
                {
                    obj = map.Object;
                    editingMap = map;
                }
                first = false;
                if (column >= map.Line)
                {
                    obj = map.Object;
                    editingMap = map;
                }
            }
            return obj;
        }

        private bool firstProperties = true;
        public override void Draw(double elapsed)
        {
            //Main Window Contents
            if (propertiesOpen && !statePlaying)
            {
                ImGui.Columns(2, "##alecolumns", true);
                if (firstProperties)
                {
                    ImGui.SetColumnWidth(0, 300);
                    firstProperties = false;
                }
                ImGui.BeginChild("##leftpanel");
                DoProperties();
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            if (TabHandler.VerticalTab(Icons.PenSquare, "Properties", VerticalTabStyle.IconAndText, propertiesOpen)) propertiesOpen = !propertiesOpen;
            //Viewport
            ImGui.SameLine();
            ImGui.BeginChild("##maincontent");
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref h1, ref h2, 8, 8, -1);
            h1 = totalH - h2 - 24f;
            ImGui.BeginChild("###viewport",new Vector2(-1,h1));
            if(validXml)
                DoViewport(mainWindow.RenderDelta);
            else
                ImGui.Text(exceptionText);
            ImGui.EndChild();
            //Text
            ImGui.BeginChild("###text", new Vector2(-1, h2));

            xmlEditor.Render("##texteditor");
            if (xmlEditor.TextChanged())
                TextChanged();
            var coords = xmlEditor.GetCoordinates();
            editingObject = null;
            if (validXml)
            {
                editingObject = FindEditingObject(coords.Y + 1, coords.X + 1);
            }
            ImGui.EndChild();
            ImGui.EndChild();
        }

        private RenderTarget2D renderTarget;
        private ImTextureRef renderTargetImage;
        private int rtX = -1, rtY = -1;
        private bool lastDown = false;
        void DoViewport(double delta)
        {
            var szX = (int)ImGui.GetContentRegionAvail().X;
            var szY = (int) ImGui.GetWindowHeight();
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
            //Do drawing
            if (widget != null)
            {
                context.GlobalTime = mainWindow.TotalTime;
                context.ViewportWidth = rtX;
                context.ViewportHeight = rtY;
                context.RenderWidget(delta);
            }
            //
            mainWindow.RenderContext.PopViewport();
            mainWindow.RenderContext.RenderTarget = null;
            //We don't use ImageButton because we need to be specific about sizing
            var cPos = ImGui.GetCursorPos();
            ImGui.Image(renderTargetImage, new Vector2(rtX, rtY), new Vector2(0, 1), new Vector2(1, 0));
            ImGui.SetCursorPos(cPos);
            var wPos = ImGui.GetWindowPos();
            var mX = (int) (mainWindow.Mouse.X - cPos.X - wPos.X);
            var mY = (int) (mainWindow.Mouse.Y - cPos.Y - wPos.Y);
            ImGui.InvisibleButton("##renderThing", new Vector2(rtX, rtY));
            if (ImGui.IsItemHovered())
            {
                context.Update(null, mainWindow.TotalTime, mX, mY, false);
                if(ImGui.IsItemClicked(0)) context.OnMouseClick();
                var isDown = ImGui.IsMouseDown(0);
                if (lastDown && !isDown) context.OnMouseUp();
                if (isDown && !lastDown) context.OnMouseDown();
                context.MouseLeftDown = isDown;
                lastDown = isDown;
            }
            else {
                context.Update(null, mainWindow.TotalTime, 0, 0, false);
                context.MouseLeftDown = false;
                if (lastDown)
                {
                    lastDown = false;
                    context.OnMouseUp();
                }
            }
        }

        private bool statePlaying = false;

        void DoProperties()
        {
            propertyGrid.SetEditingObject(editingObject);
            if (propertyGrid.Draw()) //property changed!
            {
                UiXmlWriter.FillSimpleProperties(editingMap.Element, editingObject);
                var text = xmlEditor.GetText();
                text = ReplaceXml(text, editingMap.Element.ToString());
                xmlEditor.SetText(text.TrimEnd('\n'));
                TextChanged();
            }
        }

        string ReplaceXml(string text, string newElement)
        {
            var start = GetStart(text);
            var scanText = text.Substring(start);
            var stringReader = new StringReader(scanText);
            var reader = new XmlTextReader(stringReader);
            reader.MoveToContent();
            int startDepth = reader.Depth;
            if (!reader.IsEmptyElement) {
                reader.Read();
                while (!reader.EOF && (reader.Depth > startDepth))
                    reader.Read();
            }
            var idx2 = start + GetIndex(scanText, reader.LineNumber, reader.LinePosition) - 1;
            var end = text.IndexOf('>', idx2) + 1;
            return text.Substring(0, start) + newElement + text.Substring(end);
        }

        int GetIndex(string text, int line, int column)
        {
            int cLine = 1;
            int index = 0;
            while (cLine < line)
            {
                if (text[index++] == '\n') cLine++;
                if (index >= text.Length) return text.Length - 1;
            }
            var x = index + column;
            return x;

        }
        int GetStart(string text)
        {
            var linfo = ((IXmlLineInfo) editingMap.Element);
            var x = GetIndex(text, linfo.LineNumber, linfo.LinePosition);
            x -= 3;
            var start = x < 0 ? 0 : x;
            start = text.IndexOf('<', start);
            return start;
        }

        private string exceptionText = "Nothing typed yet";
        void TextChanged()
        {
            var text = xmlEditor.GetText();
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
            {
                objectMaps = null;
                validXml = false;
                widget = null;
                exceptionText = "Nothing typed yet";
                return;
            }
            try
            {
                objectMaps = new List<XmlObjectMap>();
                widget = (UiWidget) mainWindow.Project.XmlLoader.FromString(text, objectMaps);
                if(mainWindow.Project.UiData.Stylesheet != null) widget.ApplyStylesheet(mainWindow.Project.UiData.Stylesheet);
                context.SetWidget(widget);
                validXml = true;
            }
            catch (Exception ex)
            {
                validXml = false;
                exceptionText = $"Invalid XML\n\n{ex.Message}\n{ex.StackTrace}";
            }
        }
        public override void Dispose()
        {
            if (renderTarget != null)
            {
                ImGuiHelper.DeregisterTexture(renderTarget.Texture);
                renderTarget.Dispose();
            }
            xmlEditor.Dispose();
        }
    }
}
