using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups
{
    public class FrameRectEditorPopup : PopupWindow
    {
        readonly LUtfNode selectedNode;
        public FrameRectEditorPopup(LUtfNode selectedNode)
        {
            this.selectedNode = selectedNode;

            try
            {
                frameRectsOriginal = FrameRectCalculator.ParseFrameRects(selectedNode.Data);
                frameRectsUpdated = new List<FrameRect>(frameRectsOriginal);
            }
            catch (Exception ex)
            {
                sourceDataError = true;
                sourceDataErrorMessage = ex.Message;

            }


        }

        public override string Title { get; set; } = "Frame Rect Editor";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        public override bool NoClose => false;
        public override Vector2 InitSize => new Vector2(400, 300);

        static readonly float LABEL_WIDTH = 125f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly float BUTTON_WIDTH_LONG = 180f;
        static readonly float BUTTON_PADDING = 16;
        static readonly float SQAURE_BUTTON_WIDTH = 30;
        float TABLE_WIDTH = 550f;
        float TABLE_HEIGHT = 300f;
        readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
        readonly Vector4 WARN_TEXT_COLOUR = new Vector4(1f, 0.86f, 0.25f, 1f);
        readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);

        int texWidth = 256;
        int texHeight = 256;
        int gridSizeX = 4;
        int gridSizeY = 4;
        int frameCount = 16;
        int selectedFrameIndex = -1;

        bool generateRect = false;
        bool sourceDataError = false;
        string sourceDataErrorMessage = "";

        List<FrameRect> frameRectsOriginal = new List<FrameRect>();
        List<FrameRect> frameRectsUpdated = new List<FrameRect>();

        public override void Draw(bool appearing)
        {
            if (generateRect)
            {
                DrawGenerateOptions();
            }
            else if (sourceDataError)
            {
                DrawErrorMessage();
            }
            else
            {
                DrawEditor();
            }
        }

        void DrawErrorMessage()
        {
            ImGui.Spacing();
            ImGui.PushFont(ImGuiHelper.Roboto, 22);
            ImGuiExt.CenterText(sourceDataErrorMessage, ERROR_TEXT_COLOUR);
            ImGui.PopFont();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGuiExt.CenterText("New frame rect data needs to be generated.");
            ImGuiExt.CenterText("This will delete any existing data on the selected node.", WARN_TEXT_COLOUR);
            ImGui.NewLine();
            ImGuiExt.CenterText("Do you want to continue?");

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();
            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + BUTTON_PADDING * ImGuiHelper.Scale);
            if (ImGui.Button("Continue", new Vector2(BUTTON_WIDTH, 0)))
            {
                generateRect = true;
                sourceDataError = false;
                sourceDataErrorMessage = "";
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX((avail - BUTTON_WIDTH - BUTTON_PADDING) * ImGuiHelper.Scale);
            if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.Spacing();
        }
        void DrawGenerateOptions()
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Texture size");
            ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

            ImGui.InputInt("##TextureSizeX", ref texWidth);
            ImGui.SameLine();
            ImGui.InputInt("##TextureSizeY", ref texHeight);

            ImGui.Spacing();

            ImGui.Text("Grid size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##GridSizeX", ref gridSizeX);
            ImGui.SameLine();
            ImGui.InputInt("##GridSizeY", ref gridSizeY);
            ImGui.Spacing();
            ImGui.Text("Frame Count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##frameCount", ref frameCount);

            ImGui.NewLine();
            ImGui.Separator();
            ImGui.NewLine();

            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));
            ImGui.SameLine();
            if (ImGui.Button("Generate Frame Rects", new Vector2(BUTTON_WIDTH_LONG, 0)))
            {
                frameRectsUpdated = FrameRectCalculator.GenerateFrameRects(gridSizeX, gridSizeY, frameCount);
                generateRect = false;
            }
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH_LONG, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing()));

        }
        void DrawEditor()
        {
            float scale = ImGuiHelper.Scale;
            float buttonColWidth = SQAURE_BUTTON_WIDTH + 16 * scale;

            // ---------- Top bar ----------
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Frames: {frameRectsUpdated.Count}");


            ImGui.SameLine();
            float btnWidth = 220 * scale;
            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - btnWidth - BUTTON_PADDING);

            if (ImGui.Button("Generate New Frame Rect Data", new Vector2(btnWidth, 0)))
            {
                generateRect = true;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // ---------- Table ----------
            if (ImGui.BeginTable(
                "##FrameRectTable",
                6,
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.ScrollY,
                new Vector2(TABLE_WIDTH, TABLE_HEIGHT)))
            {
                ImGui.TableSetupColumn("Frame #", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("U0", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("V0", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("U1", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("V1", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < frameRectsUpdated.Count; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(i);

                    bool selected = selectedFrameIndex == i;

                    // ---- Frame number (selectable row) ----
                    ImGui.TableNextColumn();

                    ImGui.AlignTextToFramePadding();
                    if (ImGui.Selectable(
                        $"{i + 1}",
                        selected,
                        ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
                    {
                        selectedFrameIndex = i;
                    }

                    var rect = frameRectsUpdated[i];

                    ImGui.TableNextColumn();

                    int index = (int)rect.Index;
                    ImGui.PushItemWidth(-1);
                    if (ImGui.InputInt("##idx", ref index))
                    {
                        rect.Index = (uint)Math.Max(0, index);
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##u0", ref rect.U0, 0, 0, "%.3f");

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##v0", ref rect.V0, 0, 0, "%.3f");

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##u1", ref rect.U1, 0, 0, "%.3f");

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputFloat("##v1", ref rect.V1, 0, 0, "%.3f");

                    frameRectsUpdated[i] = rect;
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            // ---------- Right-side buttons ----------
            ImGui.SameLine();
            ImGui.BeginChild(
                "##frameRectButtons",
                new Vector2(buttonColWidth, TABLE_HEIGHT),
                ImGuiChildFlags.None);

            ImGui.BeginDisabled(selectedFrameIndex < 0);

            if (ImGui.Button(Icons.ArrowUp.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedFrameIndex > 0)
                {
                    (frameRectsUpdated[selectedFrameIndex - 1], frameRectsUpdated[selectedFrameIndex]) =
                        (frameRectsUpdated[selectedFrameIndex], frameRectsUpdated[selectedFrameIndex - 1]);
                    selectedFrameIndex--;
                }
            }

            ImGui.Spacing();

            if (ImGui.Button(Icons.ArrowDown.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedFrameIndex < frameRectsUpdated.Count - 1)
                {
                    (frameRectsUpdated[selectedFrameIndex + 1], frameRectsUpdated[selectedFrameIndex]) =
                        (frameRectsUpdated[selectedFrameIndex], frameRectsUpdated[selectedFrameIndex + 1]);
                    selectedFrameIndex++;
                }
            }

            ImGui.EndDisabled();

            ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeightWithSpacing()));

            if (ImGui.Button(Icons.SquarePlus.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                frameRectsUpdated.Add(new FrameRect
                {
                    Index = 0,
                    U0 = 0,
                    V0 = 0,
                    U1 = 1,
                    V1 = 1
                });
                selectedFrameIndex = frameRectsUpdated.Count - 1;
            }

            ImGui.Spacing();

            ImGui.BeginDisabled(selectedFrameIndex < 0);
            if (ImGui.Button(Icons.TrashAlt.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                frameRectsUpdated.RemoveAt(selectedFrameIndex);
                selectedFrameIndex = Math.Clamp(selectedFrameIndex, 0, frameRectsUpdated.Count - 1);
            }
            ImGui.EndDisabled();

            ImGui.EndChild();

            // ---------- Bottom action area ----------
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float padding = 12 * scale;
            float bottomBtnWidth = 120 * scale;
            float startX = ImGui.GetCursorPosX();
            float bottomAvail = ImGui.GetContentRegionAvail().X;

            ImGui.SetCursorPosX(startX + padding);
            if (ImGui.Button("Apply", new Vector2(bottomBtnWidth, 0)))
            {
                selectedNode.Data = UnsafeHelpers.CastArray(frameRectsUpdated.ToArray());
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(startX + bottomAvail - bottomBtnWidth - padding);
            if (ImGui.Button("Cancel", new Vector2(bottomBtnWidth, 0)))
            {

                ImGui.CloseCurrentPopup();
            }
        }

    }
}
