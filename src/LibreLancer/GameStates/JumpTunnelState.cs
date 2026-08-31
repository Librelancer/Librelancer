// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Client.Components;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Graphics;
using LibreLancer.Media;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer;

internal sealed class JumpTunnelState : GameState
{
    private readonly CGameSession session;
    private readonly JumpClientTransition transition;
    private readonly JumpTransitionTimeline timeline;
    private readonly LookAtCamera camera = new()
    {
        ZRange = new Vector2(1, 100000),
        GameFOV = true
    };
    private SystemRenderer? renderer;
    private GameWorld? world;
    private GameObject? ship;
    private GameObject? tunnelObject;
    private JumpTunnelRenderer? tunnelRenderer;
    private ParticleEffectRenderer? tunnelEffect;
    private LoadingScreen? loader;
    private SoundInstance? tunnelSound;

    public JumpTunnelState(
        FreelancerGame game,
        CGameSession session,
        JumpClientTransition transition) : base(game)
    {
        this.session = session;
        this.transition = transition;
        timeline = new JumpTransitionTimeline(transition.Effect);
        CreateScene();
    }

    private void CreateScene()
    {
        renderer = new SystemRenderer(camera, Game.ResourceManager, Game)
        {
            ZOverride = true,
            DrawStarsphere = false,
            DrawNebulae = false,
            BackgroundOverride = new Color4(
                new Color3f(transition.Effect?.JumpBackgroundColor ?? Vector3.Zero),
                1)
        };
        renderer.SystemLighting = new SystemLighting
        {
            Ambient = new Color4(
                new Color3f(transition.Effect?.JumpAmbient ?? Vector3.One),
                1)
        };
        world = new GameWorld(
            renderer,
            Game.Sound,
            Game.ResourceManager,
            () => Game.TotalTime,
            initPhys: false);
        ship = new GameObject(
            session.PlayerShip!,
            Game.ResourceManager,
            true,
            false)
        {
            Nickname = "jump_player",
            NetID = session.PlayerNetID,
            Flags = GameObjectFlags.Player
        };
        foreach (var equipment in session.Items)
        {
            if (string.IsNullOrEmpty(equipment.Hardpoint))
                continue;
            EquipmentObjectManager.InstantiateEquipment(
                ship,
                Game.ResourceManager,
                Game.Sound,
                EquipmentType.LocalPlayer,
                equipment.Hardpoint,
                equipment.Equipment!);
        }
        if (ship.TryGetComponent<CEngineComponent>(out var engine))
            engine.Speed = 1;
        world.AddObject(ship);
        ship.Register(world);

        if (transition.Tunnel != null)
        {
            tunnelRenderer = new JumpTunnelRenderer(
                Game.RenderContext,
                Game.ResourceManager,
                transition.Tunnel,
                FLHash.CreateID(
                    $"{transition.DestinationSystem}:{transition.ExitObject}"));
            tunnelObject = new GameObject
            {
                Nickname = "jump_tunnel",
                RenderComponent = tunnelRenderer
            };
            world.AddObject(tunnelObject);
            tunnelObject.Register(world);
        }
        AttachEffect(transition.Effect?.JumpTunnelEffect);
        SetSceneTransform((float)timeline.Elapsed);
    }

    private void AttachEffect(ResolvedFx? effect)
    {
        if (effect == null || ship == null)
            return;
        var particle = effect.GetEffect(Game.ResourceManager);
        if (particle != null)
        {
            tunnelEffect = new ParticleEffectRenderer(particle);
            ship.ExtraRenderers.Add(tunnelEffect);
        }
        if (effect.Sound != null)
        {
            tunnelSound = Game.Sound.GetInstance(effect.Sound.Nickname);
            tunnelSound?.Play();
        }
    }

    private void SetSceneTransform(float elapsed)
    {
        JumpTunnelSample sample;
        if (tunnelRenderer != null && transition.Tunnel != null)
        {
            var distance = JumpTunnelMotion.DistanceAt(elapsed, transition.Tunnel);
            sample = tunnelRenderer.Geometry.SampleDistance(distance);
        }
        else
        {
            sample = new JumpTunnelSample(
                new Vector3(0, 0, elapsed * 1000),
                Vector3.UnitZ,
                Vector3.UnitY,
                Vector3.UnitX,
                100,
                Vector3.One,
                1);
        }

        var pathTransform = new Transform3D(
            sample.Center,
            QuaternionEx.LookAt(sample.Center, sample.Center + sample.Tangent));

        // The ship and camera stay in the normal chase-camera rig. The tunnel
        // moves around them, which prevents spline curvature from making the
        // ship wander around the screen.
        tunnelObject?.SetLocalTransform(pathTransform.Inverse());
        var cameraPosition = session.PlayerShip?.ChaseOffset ??
                             new Vector3(0, 18, 75);
        camera.Update(
            Game.Width,
            Game.Height,
            cameraPosition,
            -Vector3.UnitZ * 120,
            Matrix4x4.Identity);
        Game.Sound.UpdateListener(
            0,
            cameraPosition,
            -Vector3.UnitZ,
            Vector3.UnitY);
    }

    public override void Update(double delta)
    {
        if (session.Update())
            return;
        if (loader != null)
        {
            if (loader.Update(delta))
            {
                loader = null;
                timeline.BeginTunnelIn();
                CreateScene();
            }
            return;
        }

        if (timeline.Phase == JumpTransitionPhase.TunnelOut &&
            timeline.UpdateTunnelOut(delta))
        {
            BeginLoading();
            return;
        }
        if (timeline.Phase == JumpTransitionPhase.TunnelIn &&
            timeline.UpdateTunnelIn(delta))
            session.DestinationPreloaded(transition);
        if (timeline.TunnelDuration - timeline.Elapsed <=
            (transition.Effect?.KillTimeBeforeDone ?? 0))
            StopTunnelEffect();
        SetSceneTransform((float)timeline.Elapsed);
        world!.Update(delta);
    }

    private void BeginLoading()
    {
        DisposeScene();
        Game.ResourceManager.ClearTextures();
        Game.ResourceManager.ClearMeshes();
        Game.Ui.MeshDisposeVersion++;
        var destination = Game.GameData.Items.Systems.Get(transition.DestinationSystem);
        loader = destination == null
            ? new LoadingScreen(Game, EmptyLoader())
            : new LoadingScreen(
                Game,
                Game.GameData.LoadSystemResources(destination)!);
        loader.Init();
    }

    private static System.Collections.Generic.IEnumerator<object> EmptyLoader()
    {
        yield break;
    }

    public override void Draw(double delta)
    {
        if (loader != null)
        {
            Game.RenderContext.ClearColor = Color4.White;
            Game.RenderContext.ClearAll();
            loader.Draw(delta);
            return;
        }

        world!.RenderUpdate(delta);
        renderer!.Draw(
            Game.RenderContext.CurrentViewport.Width,
            Game.RenderContext.CurrentViewport.Height);
        float alpha = 0;
        if (timeline.Phase == JumpTransitionPhase.TunnelOut)
        {
            var fadeStart = Math.Max(0, timeline.TunnelOutTime - 0.75);
            if (timeline.PhaseElapsed >= fadeStart)
                alpha = timeline.TunnelOutTime <= fadeStart
                    ? 1
                    : (float)Math.Clamp(
                        (timeline.PhaseElapsed - fadeStart) /
                        (timeline.TunnelOutTime - fadeStart),
                        0,
                        1);
        }
        else if (timeline.Phase == JumpTransitionPhase.TunnelIn)
        {
            var fadeDuration = Math.Min(0.75, timeline.TunnelInTime);
            alpha = fadeDuration <= 0
                ? 0
                : 1 - (float)Math.Clamp(
                    timeline.PhaseElapsed / fadeDuration,
                    0,
                    1);
        }
        if (alpha > 0)
            Game.RenderContext.TintViewport(new Color4(1, 1, 1, alpha));
    }

    private void StopTunnelEffect()
    {
        if (ship != null && tunnelEffect != null)
            ship.ExtraRenderers.Remove(tunnelEffect);
        tunnelEffect = null;
        tunnelSound?.Stop();
        tunnelSound = null;
    }

    private void DisposeScene()
    {
        StopTunnelEffect();
        if (world != null)
        {
            RemoveObject(world, ship);
            RemoveObject(world, tunnelObject);
        }
        tunnelRenderer?.Dispose();
        tunnelRenderer = null;
        tunnelObject = null;
        renderer?.Dispose();
        renderer = null;
        world?.Dispose();
        world = null;
        ship = null;
    }

    private static void RemoveObject(GameWorld world, GameObject? obj)
    {
        if (obj == null || (obj.Flags & GameObjectFlags.Exists) == 0)
            return;
        obj.Unregister(world);
        world.RemoveObject(obj);
    }

    protected override void OnUnload() => DisposeScene();
}
