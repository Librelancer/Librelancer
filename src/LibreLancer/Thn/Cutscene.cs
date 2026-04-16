// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.Thn.Events;
using LibreLancer.World;

namespace LibreLancer.Thn;

public class Cutscene : IDisposable
{
    // Public variables
    public GameWorld World = null!;
    public SystemRenderer? Renderer;
    public GameObject? PlayerShip => scriptContext.PlayerShip;

    public CEngineComponent PlayerEngine => scriptContext.PlayerEngine;

    // Public properties
    public GameResourceManager ResourceManager => resourceManager;
    public ICamera CameraHandle => camera;
    public Dictionary<string, string> Substitutions => scriptContext.Substitutions;
    public GameObject MainObject => scriptContext.MainObject;
    public GameDataManager GameData => gameData;
    public SoundManager? SoundManager => soundManager;
    public bool Running => running;

    public double CurrentTime => currentTime;

    // Private variables
    private double currentTime = 0;

    private Dictionary<string, ThnSceneObject> sceneObjects = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ThnScriptInstance> fidgets = new(StringComparer.OrdinalIgnoreCase);
    private Game game;
    private ThnCamera camera;
    private bool spawnObjects = true;
    private List<Tuple<IDrawable, ThnSceneObject>> layers = [];
    private ThnDisplayText? text;
    private GameDataManager gameData;
    private GameResourceManager resourceManager;
    private SoundManager? soundManager;
    private bool hasScene = false;
    private bool running = false;

    private ThnScriptContext scriptContext;
    private List<ThnScriptInstance> instances = [];
    private ThnSceneObject[] starSphereObjects = [];
    public event Action<ThnScript>? ScriptFinished;

    public void OnScriptFinished(ThnScript thn) => ScriptFinished?.Invoke(thn);

    public void AddStarsphere(IDrawable drawable, ThnSceneObject obj)
    {
        layers.Add(new Tuple<IDrawable, ThnSceneObject>(drawable, obj));
    }

    public void SetDisplayText(ThnDisplayText text)
    {
        this.text = text;
    }

    public void SetAmbient(Vector3 amb)
    {
        if (hasScene)
        {
            return;
        }

        if (Renderer != null)
        {
            Renderer.SystemLighting.Ambient = new Color4(amb.X / 255f, amb.Y / 255f, amb.Z / 255f, 1);
        }

        hasScene = true;
    }

    public Cutscene(ThnScriptContext context,
        Game flgame, GameDataManager data, GameWorld world, GameResourceManager resources)
    {
        game = flgame;
        gameData = data;
        World = world;
        spawnObjects = false;
        camera = new ThnCamera(game.RenderContext.CurrentViewport);
        resourceManager = resources;
        scriptContext = context;
    }

    public Cutscene(ThnScriptContext context, SpaceGameplay gameplay)
    {
        game = gameplay.FlGame;
        gameData = gameplay.FlGame.GameData;
        World = gameplay.world;
        spawnObjects = false;
        camera = new ThnCamera(gameplay.FlGame.RenderContext.CurrentViewport);
        resourceManager = gameplay.FlGame.ResourceManager;
        soundManager = gameplay.FlGame.Sound;
        scriptContext = context;
    }

    public Cutscene(ThnScriptContext context, GameDataManager gameData, GameResourceManager resources,
        SoundManager? sound, Rectangle viewport, Game game)
    {
        scriptContext = context;
        this.soundManager = sound;
        this.gameData = gameData;
        this.resourceManager = resources;
        this.game = game;
        camera = new ThnCamera(viewport);
    }

    public void UpdateViewport(Rectangle vp, float fullRatio)
    {
        camera?.SetViewport(vp, fullRatio);
    }

    public void AddObject(ThnSceneObject obj)
    {
        sceneObjects[obj.Name] = obj;
        if (obj.Object != null)
        {
            World.AddObject(obj.Object);
        }
    }

    public void RemoveObject(ThnSceneObject obj)
    {
        if (obj.Object != null)
        {
            World.RemoveObject(obj.Object);
        }
        if (fidgets.TryGetValue(obj.Name, out var fidget))
        {
            instances.Remove(fidget);
            fidgets.Remove(obj.Name);
        }
        sceneObjects.Remove(obj.Name);
    }

    public void BeginScene(params ThnScript[] scene) => BeginScene((IEnumerable<ThnScript>)scene);

    public void BeginScene(IEnumerable<ThnScript> scene)
    {
        var scripts = scene.ToArray();
        SceneSetup(scripts);
    }

    public void FidgetScript(ThnScript scene, string targetObject)
    {
        var targeted = new ThnScript() { Duration = scene.Duration };
        foreach (var ev in scene.Events.OfType<StartMotionEvent>())
        {
            targeted.Events.Add(ev.Clone(targetObject));
        }

        fidgets[targetObject] = SceneSetup([targeted], false)[0];
    }


    private ThnScriptInstance[] SceneSetup(ThnScript[] scripts, bool resetObjects = true)
    {
        hasScene = false;
        currentTime = 0;
        if (resetObjects)
        {
            sceneObjects = new Dictionary<string, ThnSceneObject>(StringComparer.OrdinalIgnoreCase);
            layers = [];
        }

        if (spawnObjects && resetObjects)
        {
            if (Renderer != null)
            {
                Renderer.Dispose();
                World.Dispose();
            }

            Renderer = new SystemRenderer(camera, resourceManager, game);
            World = new GameWorld(Renderer, resourceManager, null, false);
        }

        if (scriptContext.SetScript != null && resetObjects)
        {
            var inst = new ThnScriptInstance(this, scriptContext.SetScript);
            inst.ConstructEntities(sceneObjects, spawnObjects);
        }

        if (resetObjects)
        {
            foreach (var inst in instances)
            {
                inst.Cleanup();
            }

            instances = [];
        }

        int startIdx = instances.Count;
        var newInstances = new ThnScriptInstance[scripts.Length];
        for (int i = 0; i < scripts.Length; i++)
        {
            var script = scripts[i];
            var ts = new ThnScriptInstance(this, script);
            ts.ConstructEntities(sceneObjects, spawnObjects && resetObjects);
            instances.Add(ts);
            newInstances[i] = ts;
        }

        if (resetObjects)
        {
            var firstCamera = sceneObjects.Values.FirstOrDefault(x => x.Camera != null);
            if (firstCamera == null)
            {
                firstCamera = sceneObjects.Values.FirstOrDefault(x => x.Camera != null);
            }

            if (firstCamera != null)
            {
                camera.Object = firstCamera;
            }
        }

        if (spawnObjects && resetObjects)
        {
            // Add starspheres in the right order
            var sorted = ((IEnumerable<Tuple<IDrawable, ThnSceneObject>>)layers).Reverse()
                .OrderBy(x => x.Item2.Entity.SortGroup).ToArray();
            Renderer!.StarSphereModels = new RigidModel[sorted.Length];
            Renderer.StarSphereWorlds = new Matrix4x4[sorted.Length];
            Renderer.StarSphereLightings = new Lighting[sorted.Length];
            starSphereObjects = new ThnSceneObject[sorted.Length];

            for (int i = 0; i < sorted.Length; i++)
            {
                Renderer.StarSphereModels[i] =
                    ((sorted[i].Item1 as IRigidModelFile)!).CreateRigidModel(true, ResourceManager);
                Renderer.StarSphereWorlds[i] = Matrix4x4.CreateFromQuaternion(sorted[i].Item2.Rotate) *
                                               Matrix4x4.CreateTranslation(sorted[i].Item2.Translate);
                Renderer.StarSphereLightings[i] = Lighting.Empty;
                starSphereObjects[i] = sorted[i].Item2;
            }

            // Add objects to the renderer
            World.RegisterAll();
        }

        for (int i = startIdx; i < instances.Count; i++)
        {
            instances[i].Update(0);
        }

        // Init
        running = true;
        return newInstances;
    }

    private void UpdateStarsphere()
    {
        if (Renderer is null)
        {
            return;
        }

        for (int i = 0; i < starSphereObjects.Length; i++)
        {
            Renderer.StarSphereWorlds[i] = Matrix4x4.CreateFromQuaternion(starSphereObjects[i].Rotate) *
                                           Matrix4x4.CreateTranslation(starSphereObjects[i].Translate);
            var ldynamic = (starSphereObjects[i].Entity.ObjectFlags & ThnObjectFlags.LitDynamic) ==
                           ThnObjectFlags.LitDynamic;
            var lambient = (starSphereObjects[i].Entity.ObjectFlags & ThnObjectFlags.LitAmbient) ==
                           ThnObjectFlags.LitAmbient;
            var nofog = starSphereObjects[i].Entity.NoFog;
            Renderer.StarSphereLightings[i] = RenderHelpers.ApplyLights(Renderer.SystemLighting,
                starSphereObjects[i].Entity.LightGroup, Vector3.Zero, float.MaxValue, null,
                lambient, ldynamic, nofog);
        }
    }

    private int lagCounter = 0;
    private int LAG_LIMIT = 5;
    private const double LAG_THRESHOLD = 1 / 20.0;

    public void Update(double delta)
    {
        if (lagCounter < LAG_LIMIT && delta > LAG_THRESHOLD)
        {
            lagCounter++;
            return;
        }

        _Update(delta);
    }

    public void _Update(double delta)
    {
        if (Running)
        {
            var pos = camera.Object!.Translate;
            var forward = Vector3.Transform(-Vector3.UnitZ, camera.Object.Rotate);
            var up = Vector3.Transform(Vector3.UnitY, camera.Object.Rotate);
            soundManager?.UpdateListener(delta, pos, forward, up);
        }

        currentTime += delta;
        foreach (var obj in sceneObjects.Values) obj.Update();
        if (text != null)
        {
            if (currentTime > text.Start)
            {
                // game.GetService<Interface.Typewriter>().PlayString(gameData.GetString(text.TextIDS));
                // text = null;
            }
        }

        foreach (var instance in instances)
        {
            instance.Update(delta);
        }

        camera.Update();
        if (Renderer != null)
        {
            World.Update(delta);
        }
    }

    public void Draw(double delta, int renderWidth, int renderHeight, ICamera? overrideCam = null)
    {
        UpdateStarsphere();

        if (Renderer == null)
        {
            return;
        }

        Renderer.Camera = overrideCam ?? camera;

        World.RenderUpdate(delta);
        Renderer.Draw(renderWidth, renderHeight);
    }

    public ThnSceneObject? GetObject(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return sceneObjects.TryGetValue(name, out var o) ? o : null;
    }

    public IEnumerable<ThnSceneObject> AllObjects => sceneObjects.Values;

    public void SetCamera(string name)
    {
        var cam = GetObject(name);
        camera.Object = cam;
        soundManager?.ResetListenerVelocity();
    }

    public void Dispose()
    {
        Renderer?.Dispose();
    }
}
