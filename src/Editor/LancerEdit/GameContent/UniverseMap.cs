// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent
{
    public class UniverseMap
    {
        private EditorSystem dragTarget = null;
        private Vector2 dragOgPos = Vector2.Zero;

        public EditorUndoBuffer UndoBuffer = new EditorUndoBuffer();

        public event Action OnChange;

        class ChangePositionAction(EditorSystem target, Vector2 old, Vector2 updated)
            : EditorModification<Vector2>(old, updated)
        {
            public readonly EditorSystem Target = target;

            public override void Set(Vector2 value) =>
                Target.Position = value;
        }

        private Vector2 accumDelta = Vector2.Zero;

        static bool Snap(Vector2 original, Vector2 delta, Vector2 snaps, out Vector2 result)
        {
            result = MathHelper.Snap(original + delta, snaps);
            return (Math.Abs(original.X - result.X) > 0.0001f ||
                    Math.Abs(original.Y - result.Y) > 0.0001f);
        }

        public string Draw(int imageId, List<EditorSystem> systems, GameDataManager gameData, List<(EditorSystem, EditorSystem)> connections, int width, int height, int offsetY)
        {
            var minPos = ImGui.GetCursorScreenPos();
            var maxPos = minPos + new Vector2(width, height);

            var drawList = ImGui.GetWindowDrawList();

            float buttonSize = (int) ((width / 838.0f) * 16f);
            if (buttonSize < 2) buttonSize = 2;

            if (imageId != -1) {
                drawList.AddImage((IntPtr) imageId, minPos, maxPos, new Vector2(0, 1), new Vector2(1, 0));
            }
            else {
                drawList.AddRectFilled(minPos, maxPos, 0xFF000000);
            }

            drawList.AddText(ImGuiHelper.Default, ImGuiHelper.Default.FontSize, minPos, 0xFFFFFFFF, "Double-click to open. Click+drag to move. Shift to disable snapping");

            float margin = 0.15f;
            var min = ImGui.GetCursorPos() + new Vector2(width, height) * margin;
            var factor = (new Vector2(width, height) * (1 - 2 * margin)) / 16f;
            var connectMin = minPos + new Vector2(width, height) * margin;
            foreach (var c in connections)
            {
                var a = connectMin + c.Item1.Position * factor;
                var b = connectMin + c.Item2.Position * factor;
                drawList.AddLine(a, b, (VertexDiffuse)Color4.CornflowerBlue, 2f);
            }

            bool grabbed = false;

            EditorSystem dragCurrent = null;
            string retVal = null;
            foreach (var sys in systems)
            {
                ImGui.SetCursorPos(min + (sys.Position * factor) - new Vector2(0.5f * buttonSize));
                ImGui.PushStyleColor(ImGuiCol.Button, Color4.LightGray);
                ImGui.Button($"###{sys.System.Nickname}", new Vector2(buttonSize));
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    retVal = sys.System.Nickname;
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) && !grabbed &&
                    (dragTarget == null || dragTarget == sys))
                {
                    grabbed = true;
                    if (dragTarget == null)
                    {
                        dragTarget = sys;
                        dragOgPos = sys.Position;
                        accumDelta = Vector2.Zero;
                    }


                    accumDelta += (ImGui.GetIO().MouseDelta / factor);
                    if (Snap(dragOgPos, accumDelta, ImGui.IsKeyDown(ImGuiKey.ModShift) ? Vector2.Zero : Vector2.One, out var newPosition))
                    {
                        sys.Position = newPosition;
                    }
                    dragCurrent = sys;
                }

                ImGui.PopStyleColor();
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{gameData.GetString(sys.System.IdsName)} ({sys.System.Nickname})");
            }

            if (dragCurrent == null && dragTarget != null)
            {
                UndoBuffer.Commit(new ChangePositionAction(dragTarget, dragOgPos, dragTarget.Position));
                OnChange?.Invoke();
                dragTarget = null;
                accumDelta = Vector2.Zero;
            }

            return retVal;
        }
    }
}
