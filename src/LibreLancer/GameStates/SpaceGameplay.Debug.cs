using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Physics;
using LibreLancer.World.Components;

namespace LibreLancer;

partial class SpaceGameplay
{
    private const string DEBUG_TEXT =
        @"{3} ({4})
Camera Position: (X: {0:0.00}, Y: {1:0.00}, Z: {2:0.00})
C# Memory Usage: {5}
Velocity: {6}
Selected Object: {7}
Pitch: {8:0.00}
Yaw: {9:0.00}
Roll: {10:0.00}
Mouse Flight: {11}
World Time: {12:F2}
";

    unsafe void DrawDebugWindow(double delta)
    {
        Game.Debug.Draw(delta, () =>
        {
            ImGui.Checkbox("Object List", ref showObjectList);
            ImGui.Text($"Object Count: {world.Objects.Count}");
            string selObj = "None";

            if (Selection.Selected != null)
            {
                if (Selection.Selected.Name == null)
                {
                    selObj = "unknown object";
                }
                else
                {
                    selObj =
                        Selection.Selected.Name?.GetName(Game.GameData, player.WorldTransform.Position) ??
                        "unknown object";
                }

                selObj = $"{selObj} ({Selection.Selected.Nickname ?? "null nickname"})";
            }

            var systemName = Game.GameData.GetString(sys.IdsName);
            var text = string.Format(DEBUG_TEXT, activeCamera.Position.X, activeCamera.Position.Y,
                activeCamera.Position.Z,
                sys.Nickname, systemName, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Velocity, selObj,
                control.Steering.X, control.Steering.Y, control.Steering.Z, mouseFlight, session.WorldTime);
            ImGui.Text(text);
            ImGui.Text(
                $"Render Resolution: {Game.RenderContext.CurrentViewport.Width}x{Game.RenderContext.CurrentViewport.Height}");
            ImGui.Text($"Player Position: {player.WorldTransform.Position}");
            ImGui.Text($"PredictionErrorPos: {player.PhysicsComponent!.PredictionErrorPos}");
            ImGui.Text($"PredictionErrorQuat: {player.PhysicsComponent!.PredictionErrorQuat} ({MathHelper.QuatError(
                player.PhysicsComponent!.PredictionErrorQuat, Quaternion.Identity)})");
            ImGui.Separator();
            pilotComponent?.ImGuiDebug();
            ImGui.Text($"crosshairHit: {crosshairHit}");
            ImGui.Separator();
            var dbgT = session.GetSelectedDebugInfo();

            if (!string.IsNullOrWhiteSpace(dbgT))
            {
                ImGui.Text(dbgT);
            }

            if (Selection.Selected?.PhysicsComponent?.Body?.Collider is ConvexMeshCollider cvx)
            {
                ImGui.Text($"selected compound children: {cvx.BepuChildCount}");
            }

            if (Selection.Selected != null)
            {
                if (Selection.Selected.TryGetComponent<ShipControlAccessComponent>(out var sca))
                {
                    ImGui.Text($"selected throttle: {sca.EnginePower}");
                    ImGui.Text("received controls (if ship is in a formation)");
                    ImGui.Text($"steering: {sca.Steering}");
                    ImGui.Text($"strafe: {sca.CurrentStrafe}");
                    ImGui.Text($"engine state: {sca.EngineState}");
                }

                ImGui.Text($"selected linear velocity: {Selection.Selected.PhysicsComponent?.Body?.LinearVelocity}");
            }

            ImGui.Text($"input queue: {session.UpdateQueueCount}");
            ImGui.Text($"tick offset: {session.LastTickOffset}");
            ImGui.Text($"dropped inputs: {session.DroppedInputs}");
            ImGui.Text($"average tick offset: {session.AverageTickOffset}");
            ImGui.Text($"interval: {session.AdjustedInterval}");
            ImGui.Text($"Client Tick: {session.WorldTick}");

            if (session.Multiplayer)
            {
                var floats = new float[session.UpdatePacketSizes.Count];

                for (int i = 0; i < session.UpdatePacketSizes.Count; i++)
                {
                    floats[i] = session.UpdatePacketSizes[i];
                }

                fixed (float* f = floats)
                {
                    if (floats.Length > 0)
                    {
                        ImGui.Text($"last ack received: {session.Acks.Tick}");
                        ImGui.Text($"update packet size: {floats[^1]}");
                        ImGui.PlotLines("update packet size", ref floats[0], floats.Length);
                    }
                }
            }
            else
            {
                ImGui.Text($"Server Tick: {session.EmbeddedServer!.Server.CurrentTick}");
            }

            ImGui.Checkbox("Draw autopilot avoidance", ref world.RenderAutopilotDebug);
            ImGui.Checkbox("Draw formation lines", ref world.RenderFormationDebug);

            bool hasDebug = world.Physics!.DebugRenderer != null;
            ImGui.Checkbox("Draw hitboxes", ref hasDebug);
            ImGui.BeginDisabled(!hasDebug);
            ImGui.Checkbox("Draw raycasts", ref world.Physics.ShowRaycasts);
            ImGui.EndDisabled();

            if (hasDebug)
            {
                world.Physics.DebugRenderer = sysrender.DebugRenderer;
            }
            else
            {
                world.Physics.DebugRenderer = null;
            }

            ImGui.Text($"Free Audio Voices: {Game.Audio.FreeSources}");
            ImGui.Text($"Playing Sounds: {Game.Audio.PlayingInstances}");
            ImGui.Text($"Audio Update Time: {Game.Audio.UpdateTime:0.000}ms");

            if (!session.Multiplayer)
            {
                ImGui.Text(
                    $"Storyline: {session.EmbeddedServer!.Server.LocalPlayer!.Story?.CurrentStory?.Nickname}");
            }
            // ImGuiNET.ImGui.Text(pilotcomponent.ThrottleControl.Current.ToString());
        }, () =>
        {
            Game.Debug.MissionWindow(session.GetTriggerInfo());

            if (showObjectList)
            {
                Game.Debug.ObjectsWindow(world.Objects);
            }
        });
    }

    private void DrawSelectedFormationLine()
    {
        if (!world.RenderFormationDebug || Selection.Selected is not { } selected)
            return;

        Vector3 shipPosition;
        Vector3 targetPosition;
        if (!session.TryGetFormationLine(selected.NetID, out shipPosition, out targetPosition))
        {
            var formation = selected.Formation ??
                            (player.Formation?.Contains(selected) == true ? player.Formation : null);
            if (formation == null)
                return;
            shipPosition = selected.PhysicsComponent?.Body?.Position ?? selected.WorldTransform.Position;
            targetPosition = formation.GetShipPosition(selected);
        }

        const float markerSize = 22;
        world.DrawFormationDebug(targetPosition);
        world.DrawFormationDebugLine(shipPosition, targetPosition, Color4.Cyan);
        world.DrawFormationDebugLine(targetPosition - Vector3.UnitX * markerSize,
            targetPosition + Vector3.UnitX * markerSize, Color4.Cyan);
        world.DrawFormationDebugLine(targetPosition - Vector3.UnitY * markerSize,
            targetPosition + Vector3.UnitY * markerSize, Color4.Cyan);
        world.DrawFormationDebugLine(targetPosition - Vector3.UnitZ * markerSize,
            targetPosition + Vector3.UnitZ * markerSize, Color4.Cyan);
    }
}
