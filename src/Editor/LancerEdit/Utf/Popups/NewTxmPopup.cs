using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using BepuPhysics.Constraints;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups
{
    public class NewTxmPopup : PopupWindow
    {
        Action<KeyValuePair<String, EditableUtf>> action;
        readonly MainWindow win;

        public NewTxmPopup(MainWindow win, Action<KeyValuePair<String, EditableUtf>> action)
        {
            this.action = action;
            this.win = win;
        }

        public override string Title { get; set; } = "Create new .txm Document";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;
        public override Vector2 InitSize => new Vector2(450, 600);

        static readonly float LABEL_WIDTH = 125f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly float SQAURE_BUTTON_WIDTH = 30f;
        static readonly int MAX_LODS = 9;

        // ui state vars
        string filename = "";
        string nodeName = "";
        string[] lodPaths = new string[MAX_LODS];
        int selectedIndex = -1;
        int texWidth = 256;
        int texHeight = 256;
        int gridSizeX = 4;
        int gridSizeY = 4;
        int textureCount = 1;
        int frameCount = 16;
        int fps = 30;


        public override void Draw(bool appearing)
        {
            //TODO: Change to Center Text when merged
            ImGui.PushFont(ImGuiHelper.Roboto, 20);
            ImGui.Text("File Metadata");
            ImGui.PopFont();
            ImGui.Spacing();

            DrawFileMetadataFields();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            //TODO: Change to Center Text when merged
            ImGui.PushFont(ImGuiHelper.Roboto, 20);
            ImGui.Text("Import MIPS");
            ImGui.PopFont();
            ImGui.Spacing();

            DrawMipsFileSelect();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            

            DrawAnimFields();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();

            if (ImGui.Button("Create", new Vector2(BUTTON_WIDTH, 0)))
            {
                //TODO
                if (ValidateInput()){
                    action = GenerateUtfFileTemplate();
                }
            }
        }

        private Action<KeyValuePair<string, EditableUtf>> GenerateUtfFileTemplate()
        {
            throw new NotImplementedException();
        }

        bool ValidateInput()
        {
            throw new NotImplementedException();
        }

        private void DrawAnimFields()
        {
            //TODO: Change to Center Text when merged
            ImGui.PushFont(ImGuiHelper.Roboto, 20);
            ImGui.Text("Animation and Frame Rect Settings");
            ImGui.PopFont();
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Texture size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            unsafe
            {
                fixed (int* v = &texWidth)
                {
                    // v[0] -> texWidth
                    // v[1] -> texHeight
                    v[1] = texHeight;

                    if (ImGui.InputInt2("##TextureSize", v, ImGuiInputTextFlags.CharsDecimal))
                    {
                        texWidth = v[0];
                        texHeight = v[1];
                    }
                }
            }
            ImGui.Text("Texture count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##txtCount", ref textureCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("Frame count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##frameCount", ref frameCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("FPS"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##FPS", ref fps, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("Grid size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            unsafe
            {
                fixed (int* v = &gridSizeX)
                {
                    // v[0] -> texWidth
                    // v[1] -> texHeight
                    v[1] = gridSizeY;

                    if (ImGui.InputInt2("##gridSize", v, ImGuiInputTextFlags.CharsDecimal))
                    {
                        texWidth = v[0];
                        texHeight = v[1];
                    }
                }
            }
        }

        private void DrawMipsFileSelect()
        {
            if (ImGui.Button("Select Files", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            {
                win.QueueUIThread(() =>
                {
                    FileDialog.OpenMultiple(paths =>
                    {
                        if (paths == null || paths.Length == 0)
                        {
                            return;
                        }

                        foreach (var path in paths)
                        {
                            // Skip duplicates
                            if (lodPaths.Contains(path))
                                continue;

                            // Find first empty slot
                            for (int i = 0; i < MAX_LODS; i++)
                            {
                                if (lodPaths[i] == null)
                                {
                                    lodPaths[i] = path;
                                    break;
                                }
                            }
                        }
                    });
                });

            }
            ImGui.Spacing();
            if (ImGui.BeginTable(
                "##LODTable",
                2,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg,
                new Vector2(ImGui.GetContentRegionAvail().X - 40 * ImGuiHelper.Scale, 200)))

            {
                ImGui.TableSetupColumn("MIP", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                for (int i = 0; i < MAX_LODS; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(i);

                    bool selected = (selectedIndex == i);

                    // ---- Column 0: MIPS index ----
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Selectable($"MIPS{i}", selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
                    {
                        selectedIndex = i;
                    }

                    // ---- Column 1: filename or empty ----
                    ImGui.TableSetColumnIndex(1);
                    if (lodPaths[i] != null)
                    {
                        ImGui.Text(Path.GetFileName(lodPaths[i]));
                    }
                    else
                    {
                        ImGui.TextDisabled("<empty>");
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.SameLine();
            ImGui.BeginChild(
                "##buttons",
                new Vector2(SQAURE_BUTTON_WIDTH + 16, 200),
                ImGuiChildFlags.None);
            ImGui.BeginDisabled(selectedIndex < 0);
            if (ImGui.Button(Icons.ArrowUp.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedIndex > 0)
                {
                    (lodPaths[selectedIndex - 1], lodPaths[selectedIndex]) =
                        (lodPaths[selectedIndex], lodPaths[selectedIndex - 1]);

                    selectedIndex--;
                }
            }

            if (ImGui.Button(Icons.ArrowDown.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedIndex < MAX_LODS - 1)
                {
                    (lodPaths[selectedIndex + 1], lodPaths[selectedIndex]) =
                        (lodPaths[selectedIndex], lodPaths[selectedIndex + 1]);

                    selectedIndex++;
                }
            }
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));
            if (ImGui.Button(Icons.TrashAlt.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                lodPaths[selectedIndex] = null;
                selectedIndex = -1;
            }

            ImGui.EndDisabled();
            ImGui.EndChild();
        }

        private void DrawFileMetadataFields()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Filename"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

            ImGui.AlignTextToFramePadding();
            ImGui.PushItemWidth(-1);
            ImGui.InputTextWithHint("##filename", "Untitled", ref filename, 4068);
            ImGui.PopItemWidth();

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Node name"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.PushItemWidth(-1);
            ImGui.InputTextWithHint("##nodeName", "untitledAnim", ref nodeName, 4068);
            ImGui.PopItemWidth();
        }
    }
}
