// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.IO;
using LibreLancer.ImUI;

namespace InterfaceEdit
{
    public class ScriptEditor : SaveableTab
    {
        private string path;
        private ColorTextEdit textEditor;

        public override string Filename => path;

        public ScriptEditor(string path)
        {
            this.path = path;
            Title = Path.GetFileName(path);
            textEditor = new ColorTextEdit();
            textEditor.SetMode(ColorTextEditMode.Lua);
            textEditor.SetText(File.ReadAllText(path));
        }

        public override void Save()
        {
            File.WriteAllText(path, textEditor.GetText());
        }

        public override void Draw(double elapsed)
        {
            textEditor.Render("##textEditor");
        }

        public override void Dispose()
        {
            textEditor.Dispose();
        }
    }
}