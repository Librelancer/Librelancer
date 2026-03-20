// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.Thn.Events;
using LibreLancer.Utf.Dfm;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Thn
{
    public abstract class ThnEventProcessor
    {
        public abstract bool Run(double delta);
    }

    public class ThnScriptInstance
    {
        private Queue<ThnEvent> events = new();
        private List<ThnEventProcessor> processors = [];

        public double CurrentTime = 0;
        public double Duration;

        public bool Running => CurrentTime < Duration;

        public Cutscene Cutscene;

        public Dictionary<string, ThnObject> Objects = [];
        public Dictionary<string, ThnSoundInstance> Sounds = new();

        private readonly ThnScript thn;

        public ThnScriptInstance(Cutscene cs, ThnScript script)
        {
            this.thn = script;
            Duration = script.Duration;
            Cutscene = cs;

            foreach (var ev in script.Events)
            {
                events.Enqueue(ev);
            }
        }

        public void AddProcessor(ThnEventProcessor ev)
        {
            processors.Add(ev);
        }

        private bool CheckObject(ThnEntity e, object? sub, EntityTypes type, string templateName)
        {
            return sub != null && type == e.Type && e.Template.Equals(templateName, StringComparison.OrdinalIgnoreCase);
        }

        public void ConstructEntities(Dictionary<string, ThnObject> objects, bool spawnObjects)
        {
            this.Objects = objects;
            if (spawnObjects && Cutscene.PlayerShip != null)
                Cutscene.PlayerShip.World = Cutscene.World;
            List<ThnObject> monitors = [];

            foreach (var kv in thn.Entities)
            {
                if (Objects.ContainsKey(kv.Key)) continue;
                if ((kv.Value.ObjectFlags & ThnObjectFlags.Reference) == ThnObjectFlags.Reference) continue;
                var obj = new ThnObject
                {
                    Name = kv.Key,
                    Translate = kv.Value.Position ?? Vector3.Zero,
                    Rotate = kv.Value.Rotation
                };

                // PlayerShip object
                if (spawnObjects && CheckObject(kv.Value, Cutscene.PlayerShip, EntityTypes.Compound, "playership"))
                {
                    obj.Object = Cutscene.PlayerShip;
                    obj.Object!.RenderComponent!.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) ==
                                                              ThnObjectFlags.LitDynamic;
                    obj.Object.RenderComponent.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) ==
                                                            ThnObjectFlags.LitAmbient;
                    obj.Object.RenderComponent.NoFog = kv.Value.NoFog;
                    ((ModelRenderer) obj.Object.RenderComponent).LightGroup = kv.Value.LightGroup;
                    obj.Entity = kv.Value;
                    Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                    obj.Object.SetLocalTransform(new Transform3D(transform, obj.Rotate));
                    obj.HpMount = Cutscene.PlayerShip!.GetHardpoint("HpMount");
                    Cutscene.World.AddObject(obj.Object);
                    Objects.Add(kv.Key, obj);
                    continue;
                }

                if (spawnObjects && CheckObject(kv.Value, Cutscene.PlayerEngine, EntityTypes.PSys, "PlayerShipEngines"))
                {
                    obj.Entity = kv.Value;
                    obj.Engine = Cutscene.PlayerEngine;
                    Objects.Add(kv.Key, obj);
                    continue;
                }

                var template = kv.Value.Template;
                if (Cutscene.Substitutions != null &&
                    Cutscene.Substitutions.TryGetValue(kv.Value.Template, out var replacement))
                    template = replacement;
                var resman = Cutscene.ResourceManager;
                var gameData = Cutscene.GameData;

                if (spawnObjects && kv.Value.Type == EntityTypes.Compound)
                {
                    bool getHpMount = false;
                    // Fetch model
                    IDrawable drawable = null!;
                    float[]? lodranges = null;

                    if (!string.IsNullOrEmpty(template))
                    {
                        switch (kv.Value.MeshCategory!.ToLowerInvariant())
                        {
                            case "solar":
                                ModelResource? mr;
                                (mr, lodranges) = gameData.GetSolar(template);
                                drawable = mr!.Drawable;
                                break;
                            case "ship":
                            case "spaceship":
                                getHpMount = true;
                                var sh = gameData.Items.Ships.Get(template)!;
                                drawable = sh.ModelFile!.LoadFile(resman)!.Drawable;
                                break;
                            case "prop":
                                drawable = gameData.GetProp(template)!;
                                break;
                            case "room":
                                drawable = gameData.GetRoom(template)!;
                                break;
                            case "equipment cart":
                                drawable = gameData.GetCart(template)!;
                                break;
                            case "equipment":
                                var eq = gameData.Items.Equipment.Get(template);
                                drawable = eq?.ModelFile!.LoadFile(resman)!.Drawable!;
                                break;
                            case "asteroid":
                                var ast = gameData.Items.Asteroids.Get(template);
                                drawable = ast?.ModelFile!.LoadFile(resman)!.Drawable!;
                                break;
                            default:
                                throw new NotImplementedException("Mesh Category " + kv.Value.MeshCategory);
                        }
                    }
                    else
                    {
                        FLLog.Warning("Thn", $"object '{kv.Value.Name}' has empty template, category " +
                                             $"'{kv.Value.MeshCategory}'");
                    }

                    if (kv.Value.UserFlag != 0)
                    {
                        // This is a starsphere
                        Cutscene.AddStarsphere(drawable, obj);
                    }
                    else
                    {
                        obj.Object = new GameObject(new ModelResource(drawable, default), Cutscene.ResourceManager,
                            true, false)
                        {
                            Name = new ObjectName(kv.Value.Name)
                        };

                        if (getHpMount)
                            obj.HpMount = obj.Object.GetHardpoint("HpMount");

                        if (obj.Object.RenderComponent is ModelRenderer r)
                        {
                            r.LightGroup = kv.Value.LightGroup;
                            r.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) ==
                                           ThnObjectFlags.LitDynamic;
                            r.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) ==
                                           ThnObjectFlags.LitAmbient;
                            // HIDDEN just seems to be an editor flag?
                            // r.Hidden = (kv.Value.ObjectFlags & ThnObjectFlags.Hidden) == ThnObjectFlags.Hidden;
                            r.NoFog = kv.Value.NoFog;
                            r.LODRanges = lodranges;
                        }
                    }
                }
                else if (kv.Value.Type == EntityTypes.PSys)
                {
                    var fx = gameData.Items.Effects.Get(kv.Value.Template);
                    fx ??= gameData.Items.VisEffects.Get(kv.Value
                        .Template); // TODO: Check if this only searches VisEffects

                    if (fx?.AlePath != null)
                    {
                        obj.Object = new GameObject
                        {
                            RenderComponent = new ParticleEffectRenderer(fx.GetEffect(resman)) { Active = false }
                        };
                    }
                }
                else if (kv.Value.Type == EntityTypes.Scene)
                {
                    if (kv.Value.DisplayText != null)
                        Cutscene.SetDisplayText(kv.Value.DisplayText);

                    var amb = kv.Value.Ambient!.Value;
                    if (amb is { X: 0, Y: 0, Z: 0 }) continue;
                    Cutscene.SetAmbient(amb);
                }
                else if (kv.Value.Type == EntityTypes.Light)
                {
                    var lt = new DynamicLight
                    {
                        LightGroup = kv.Value.LightGroup,
                        Active = kv.Value.LightProps!.On,
                        Light = kv.Value.LightProps.Render
                    };
                    obj.Light = lt;
                    obj.LightDir = lt.Light.Direction;
                    lt.Light.Direction = Vector3.Transform(lt.Light.Direction, obj.Rotate);
                    if (Cutscene.Renderer != null)
                        Cutscene.Renderer.SystemLighting.Lights.Add(lt);
                }
                else if (kv.Value.Type == EntityTypes.Camera)
                {
                    obj.Camera = new ThnCameraProps();
                    obj.Camera.FovH = kv.Value.FovH ?? obj.Camera.FovH;
                    obj.Camera.AspectRatio = kv.Value.HVAspect ?? obj.Camera.AspectRatio;
                    if (kv.Value.NearPlane != null) obj.Camera.Znear = kv.Value.NearPlane.Value;
                    if (kv.Value.FarPlane != null) obj.Camera.Zfar = kv.Value.FarPlane.Value;
                }
                else if (kv.Value.Type == EntityTypes.Marker)
                {
                    obj.Object = new GameObject
                    {
                        Name = new ObjectName("Marker"),
                        Nickname = ""
                    };

                    if (kv.Value.MainObject && Cutscene.MainObject != null)
                    {
                        obj.Object.Parent = Cutscene.MainObject;
                        obj.Object.AddComponent(new DirtyTransformComponent(obj.Object));
                        obj.PosFromObject = true;
                        obj.Translate = Cutscene.MainObject.WorldTransform.Position;
                        obj.Rotate = Cutscene.MainObject.WorldTransform.Orientation;
                    }
                }
                else if (kv.Value.Type == EntityTypes.Deformable)
                {
                    // TODO: Hacky with fidget/placement scripts
                    if (string.IsNullOrEmpty(kv.Value.Actor) || !objects.ContainsKey(kv.Value.Actor))
                    {
                        obj.Object = new GameObject();
                        gameData.GetCostume(template, out var body, out var head, out var leftHand,
                            out var rightHand);
                        var skel = new DfmSkeletonManager(body?.LoadModel(resman)!, head?.LoadModel(resman),
                            leftHand?.LoadModel(resman), rightHand?.LoadModel(resman))
                        {
                            FloorHeight = kv.Value.FloorHeight
                        };
                        obj.Object.RenderComponent = new CharacterRenderer(skel);
                        var anmComponent = new AnimationComponent(obj.Object, gameData.GetCharacterAnimations());
                        obj.Object.AnimationComponent = anmComponent;
                        obj.Object.AddComponent(anmComponent);
                    }
                    else
                    {
                        obj.Actor = kv.Value.Actor;

                        if (Objects.TryGetValue(obj.Actor, out var act))
                        {
                            act.Translate = obj.Translate;
                            act.Rotate = obj.Rotate;
                            act.Update();
                        }
                    }
                }
                else if (kv.Value.Type == EntityTypes.Sound)
                {
                    obj.Sound = new ThnSound(kv.Value.Template, Cutscene.SoundManager!, kv.Value.AudioProps!, obj)
                    {
                        Spatial = (kv.Value.ObjectFlags & ThnObjectFlags.SoundSpatial) == ThnObjectFlags.SoundSpatial
                    };
                }
                else if (kv.Value.Type == EntityTypes.Monitor)
                {
                    monitors.Add(obj);
                }

                if (obj.Object != null)
                {
                    if (!obj.PosFromObject)
                    {
                        Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                        obj.Object.SetLocalTransform(new Transform3D(transform, kv.Value.Rotation));
                    }

                    Cutscene.World.AddObject(obj.Object);
                }

                obj.Entity = kv.Value;
                Objects[kv.Key] = obj;
            }

            // Verify? This seems to work
            monitors.Sort((x, y) => string.Compare(x.Entity.Priority, y.Entity.Priority, StringComparison.Ordinal));
            for (int i = 0; i < monitors.Count; i++)
                monitors[i].MonitorIndex = i;
        }

        private Queue<ThnEvent> delaySoundEvents = new();

        public void Update(double delta)
        {
            if (CurrentTime > Duration) return;
            CurrentTime += delta;
            // Don't run sound on T=0 exactly to avoid desync
            while (delaySoundEvents.Count > 0 && CurrentTime > 0)
                delaySoundEvents.Dequeue().Run(this);

            while (events.Count > 0 && events.Peek().Time <= CurrentTime)
            {
                var ev = events.Dequeue();

                if (delta <= 0 && (ev is StartSoundEvent || ev is StartAudioPropAnimEvent))
                {
                    delaySoundEvents.Enqueue(ev);
                }
                else
                {
                    ev.Run(this);
                }
            }

            for (int i = 0; i < processors.Count; i++)
            {
                if (!processors[i].Run(delta))
                {
                    processors.RemoveAt(i);
                    i--;
                }
            }

            if (CurrentTime > Duration)
                Shutdown();
        }

        public void Shutdown()
        {
            Cutscene.OnScriptFinished(thn);
            Cleanup();
        }

        public void Cleanup()
        {
            foreach (var v in Sounds.Values)
            {
                if (v.Instance != null)
                {
                    v.Instance.Stop();
                    v.Instance = null;
                }
            }

            Sounds = new Dictionary<string, ThnSoundInstance>();
        }
    }
}
