using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Thn;
using LibreLancer.Thorn;
using System.IO;
using System.Numerics;

namespace LancerEdit
{
    public class ThornTab : EditorTab
    {
        private readonly MainWindow window;
        private readonly ColorTextEdit colorTextEdit;
        private string lastError = null;
        private bool showErrorPopUp = false;
        private string filePath;

        public ThornTab(MainWindow win, string file, string name)
        {
            window = win;
            DocumentName = name;
            Title = name;
            filePath = file;
            colorTextEdit = new ColorTextEdit();
            colorTextEdit.SetMode(ColorTextEditMode.Lua);

            if (filePath != null)
                colorTextEdit.SetText(ThnDecompile.Decompile(file));
        }

        public override void Draw()
        {
            if (ImGui.Button("Compile"))
            {
                window.Confirm("Would you like to Compile and Overwrite the Thorn file?", () => CompileAndSave());
            }
            if (lastError != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Show Last Error"))
                {
                    showErrorPopUp = true;
                }
            }

            colorTextEdit.Render("##ColorTextEditor");

            if (lastError != null && showErrorPopUp)
            {
                ImGui.SetNextWindowSize(new Vector2(600, 300), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Compile Error"))
                {
                    if (ImGui.Button("Ok"))
                    {
                        showErrorPopUp = false;
                    }
                    ImGui.SetNextItemWidth(-1);
                    var th = ImGui.GetWindowHeight() - 100;
                    ImGui.PushFont(ImGuiHelper.SystemMonospace);
                    ImGui.InputTextMultiline("##lastError", ref lastError, uint.MaxValue, new Vector2(0, th),
                        ImGuiInputTextFlags.ReadOnly);
                    ImGui.PopFont();
                    ImGui.EndTabItem();
                }
            }
        }

        private void CompileAndSave()
        {
            var source = colorTextEdit.GetText();
            try
            {
                var compiledBytes = LuaCompiler.Compile(source, "");
                File.WriteAllBytes(filePath, compiledBytes);
                lastError = null;
            }
            catch (LuaCompileException lce)
            {
                lastError = lce.Message;
                showErrorPopUp = true;
            }
        }
    }
}
