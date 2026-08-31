// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client;

internal sealed class JumpGateEffectController(
    FreelancerGame game,
    GameWorld world) : IDisposable
{
    private sealed class ActiveGate
    {
        public required GameObject Gate;
        public required JumpGateEffect Definition;
        public required JumpGateEffectPhase Phase;
        public double Elapsed;
        public int NextGlow;
        public readonly List<ParticleEffectRenderer> Renderers = [];
    }

    private readonly Dictionary<int, ActiveGate> active = [];

    public void Activate(ObjNetId gateId, JumpGateEffectPhase phase)
    {
        if (phase == JumpGateEffectPhase.Closed)
        {
            if (active.Remove(gateId.Value, out var closing))
                Remove(closing);
            return;
        }

        var gate = world.GetObject(gateId);
        if (gate == null)
        {
            FLLog.Warning("JumpEffect", $"Gate {gateId} was not found in the client world");
            return;
        }
        if (active.TryGetValue(gateId.Value, out var previous))
        {
            if (previous.Phase == phase)
                return;
            Remove(previous);
        }

        var definition = game.GameData.Items.ResolveJumpGateEffect(
            gate.SystemObject?.JumpEffect);
        if (definition == null)
            return;
        var run = new ActiveGate
        {
            Gate = gate,
            Definition = definition,
            Phase = phase,
            Elapsed = phase == JumpGateEffectPhase.InboundBurst &&
                      definition.Glows.Length > 0
                ? definition.Glows[^1].CreateTime
                : 0
        };
        active[gateId.Value] = run;
        SpawnDue(run);
    }

    private void SpawnDue(ActiveGate run)
    {
        while (run.NextGlow < run.Definition.Glows.Length &&
               run.Definition.Glows[run.NextGlow].CreateTime <= run.Elapsed)
        {
            var glow = run.Definition.Glows[run.NextGlow++];
            if (glow.Effect == null)
                continue;
            var attachment = glow.Hardpoint == null
                ? null
                : run.Gate.GetHardpoint(glow.Hardpoint);
            if (glow.Hardpoint != null && attachment == null)
            {
                FLLog.Warning("JumpEffect",
                    $"{run.Gate.Nickname} is missing glow hardpoint {glow.Hardpoint}");
                continue;
            }
            var particle = glow.Effect.GetEffect(game.ResourceManager);
            if (particle == null)
                continue;
            var renderer = new ParticleEffectRenderer(particle)
            {
                Attachment = attachment
            };
            run.Gate.ExtraRenderers.Add(renderer);
            run.Renderers.Add(renderer);
            if (glow.Effect.Sound != null)
            {
                game.Sound.GetInstance(
                    glow.Effect.Sound.Nickname,
                    0,
                    -1,
                    -1,
                    run.Gate.WorldTransform.Position)?.Play();
            }
        }
    }

    public void Update(double delta)
    {
        foreach (var run in active.Values)
        {
            run.Elapsed += delta;
            SpawnDue(run);
            foreach (var renderer in run.Renderers)
            {
                // ALE effects remain alive for as long as the server keeps the
                // gate open. Finite particle definitions are restarted instead
                // of silently disappearing midway through another ship's dock.
                if (renderer.Finished)
                    renderer.Restart();
            }
        }
    }

    private static void Remove(ActiveGate run)
    {
        foreach (var renderer in run.Renderers)
            run.Gate.ExtraRenderers.Remove(renderer);
        run.Renderers.Clear();
    }

    public void Dispose()
    {
        foreach (var run in active.Values)
            Remove(run);
        active.Clear();
    }
}
