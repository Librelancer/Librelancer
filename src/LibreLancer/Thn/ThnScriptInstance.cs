// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Render;
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
        Queue<ThnEvent> events = new Queue<ThnEvent>();
        List<ThnEventProcessor> processors = new List<ThnEventProcessor>();

        public double CurrentTime = 0;
        public double Duration;

        public bool Running => CurrentTime < Duration;

        public Cutscene Cutscene;

        public Dictionary<string, ThnObject> Objects;
        public Dictionary<string, ThnSoundInstance> Sounds = new Dictionary<string, ThnSoundInstance>();

        private ThnScript thn;
        
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

        bool CheckObject(ThnEntity e, object sub, EntityTypes type, string templateName)
        {
            return sub != null && type == e.Type && e.Template.Equals(templateName, StringComparison.OrdinalIgnoreCase);
        }
        public void ConstructEntities(Dictionary<string, ThnObject> objects, bool spawnObjects)
        {
            this.Objects = objects;
            if (spawnObjects && Cutscene.PlayerShip != null)
                Cutscene.PlayerShip.World = Cutscene.World;
            foreach (var kv in thn.Entities)
            {
                if (Objects.ContainsKey(kv.Key)) continue;
                if ((kv.Value.ObjectFlags & ThnObjectFlags.Reference) == ThnObjectFlags.Reference) continue;
                var obj = new ThnObject();
                obj.Name = kv.Key;
                obj.Translate = kv.Value.Position ?? Vector3.Zero;
                obj.Rotate = kv.Value.RotationMatrix ?? Matrix4x4.Identity;
                //PlayerShip object
                if (spawnObjects && CheckObject(kv.Value, Cutscene.PlayerShip, EntityTypes.Compound, "playership"))
                {
                    obj.Object = Cutscene.PlayerShip;
                    obj.Object.RenderComponent.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) == ThnObjectFlags.LitDynamic;
                    obj.Object.RenderComponent.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) == ThnObjectFlags.LitAmbient;
                    obj.Object.RenderComponent.NoFog = kv.Value.NoFog;
                    ((ModelRenderer)obj.Object.RenderComponent).LightGroup = kv.Value.LightGroup;
                    obj.Entity = kv.Value;
                    Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                    obj.Object.SetLocalTransform((kv.Value.RotationMatrix ?? Matrix4x4.Identity) *
                                                 Matrix4x4.CreateTranslation(transform));
                    obj.HpMount = Cutscene.PlayerShip.GetHardpoint("HpMount");
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
                string replacement;
                if (Cutscene.Substitutions != null &&
                    Cutscene.Substitutions.TryGetValue(kv.Value.Template, out replacement))
                    template = replacement;
                var resman = Cutscene.Game.GetService<ResourceManager>();
                var gameData = Cutscene.GameData;
                if (spawnObjects && kv.Value.Type == EntityTypes.Compound)
                {
                    bool getHpMount = false;
                    //Fetch model
                    IDrawable drawable = null;
                    float[] lodranges = null;
                    if (!string.IsNullOrEmpty(template))
                    {
                        switch (kv.Value.MeshCategory.ToLowerInvariant())
                        {
                            case "solar":
                                (drawable, lodranges) = gameData.GetSolar(template);
                                break;
                            case "ship":
                            case "spaceship":
                                getHpMount = true;
                                var sh = gameData.GetShip(template);
                                drawable = sh.ModelFile.LoadFile(resman);
                                break;
                            case "prop":
                                drawable = gameData.GetProp(template);
                                break;
                            case "room":
                                drawable = gameData.GetRoom(template);
                                break;
                            case "equipment cart":
                                drawable = gameData.GetCart(template);
                                break;
                            case "equipment":
                                var eq = gameData.GetEquipment(template);
                                drawable = eq?.ModelFile.LoadFile(resman);
                                break;
                            case "asteroid":
                                drawable = gameData.GetAsteroid(kv.Value.Template);
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

                    drawable?.Initialize(resman);
                    if (kv.Value.UserFlag != 0)  {
                        //This is a starsphere
                        Cutscene.AddStarsphere(drawable, obj);
                    }
                    else
                    {
                        obj.Object = new GameObject(drawable, Cutscene.Game.GetService<ResourceManager>(), true, false);
                        obj.Object.Name = new ObjectName(kv.Value.Name);
                        obj.Object.PhysicsComponent = null; //Jitter seems to interfere with directly setting orientation
                        if (getHpMount)
                            obj.HpMount = obj.Object.GetHardpoint("HpMount");
                        var r = (ModelRenderer)obj.Object.RenderComponent;
                        if (r != null)
                        {
                            r.LightGroup = kv.Value.LightGroup;
                            r.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) ==
                                           ThnObjectFlags.LitDynamic;
                            r.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) ==
                                           ThnObjectFlags.LitAmbient;
                            //HIDDEN just seems to be an editor flag?
                            //r.Hidden = (kv.Value.ObjectFlags & ThnObjectFlags.Hidden) == ThnObjectFlags.Hidden;
                            r.NoFog = kv.Value.NoFog;
                            r.LODRanges = lodranges;
                        }
                    }
                }
                else if (kv.Value.Type == EntityTypes.PSys)
                {
                    var fx = gameData.GetEffect(kv.Value.Template);
                    if (fx != null)
                    {
                        obj.Object = new GameObject();
                        obj.Object.RenderComponent = new ParticleEffectRenderer(fx.GetEffect(resman)) {Active = false};
                    }
                }
                else if (kv.Value.Type == EntityTypes.Scene)
                {
                    if(kv.Value.DisplayText != null) 
                        Cutscene.SetDisplayText(kv.Value.DisplayText);
                   
                    var amb = kv.Value.Ambient.Value;
                    if (amb.X == 0 && amb.Y == 0 && amb.Z == 0) continue;
                    Cutscene.SetAmbient(amb);
                }
                else if (kv.Value.Type == EntityTypes.Light)
                {
                    var lt = new DynamicLight();
                    lt.LightGroup = kv.Value.LightGroup;
                    lt.Active = kv.Value.LightProps.On;
                    lt.Light = kv.Value.LightProps.Render;
                    obj.Light = lt;
                    obj.LightDir = lt.Light.Direction;
                    if (kv.Value.RotationMatrix.HasValue)
                    {
                        var m = kv.Value.RotationMatrix.Value;
                        lt.Light.Direction = Vector3.TransformNormal(lt.Light.Direction, m);
                    }
                    if(Cutscene.Renderer != null)
                        Cutscene.Renderer.SystemLighting.Lights.Add(lt);
                }
                else if (kv.Value.Type == EntityTypes.Camera)
                {
                    obj.Camera = new ThnCameraTransform();
                    obj.Camera.Position = kv.Value.Position.Value;
                    obj.Camera.Orientation = kv.Value.RotationMatrix ?? Matrix4x4.Identity;
                    obj.Camera.FovH = kv.Value.FovH ?? obj.Camera.FovH;
                    obj.Camera.AspectRatio = kv.Value.HVAspect ?? obj.Camera.AspectRatio;
                    if (kv.Value.NearPlane != null) obj.Camera.Znear = kv.Value.NearPlane.Value;
                    if (kv.Value.FarPlane != null) obj.Camera.Zfar = kv.Value.FarPlane.Value;
                }
                else if (kv.Value.Type == EntityTypes.Marker)
                {
                    obj.Object = new GameObject();
                    obj.Object.Name = new ObjectName("Marker");
                    obj.Object.Nickname = "";
                    if (kv.Value.MainObject && Cutscene.MainObject != null)
                    {
                        obj.Object.Parent = Cutscene.MainObject;
                        obj.Object.Components.Add(new DirtyTransformComponent(obj.Object));
                        obj.PosFromObject = true;
                    }
                }
                else if (kv.Value.Type == EntityTypes.Deformable)
                {
                    //TODO: Hacky with fidget/placement scripts
                    if (string.IsNullOrEmpty(kv.Value.Actor) || !objects.ContainsKey(kv.Value.Actor))
                    {
                        obj.Object = new GameObject();
                        gameData.GetCostume(template, out DfmFile body, out DfmFile head, out DfmFile leftHand,
                            out DfmFile rightHand);
                        var skel = new DfmSkeletonManager(body, head, leftHand, rightHand);
                        obj.Object.RenderComponent = new CharacterRenderer(skel);
                        var anmComponent = new AnimationComponent(obj.Object, gameData.GetCharacterAnimations());
                        obj.Object.AnimationComponent = anmComponent;
                        obj.Object.Components.Add(anmComponent);
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
                    obj.Sound = new ThnSound(kv.Value.Template, Cutscene.Game.GetService<SoundManager>(), kv.Value.AudioProps, obj);
                    obj.Sound.Spatial = (kv.Value.ObjectFlags & ThnObjectFlags.SoundSpatial) == ThnObjectFlags.SoundSpatial;
                }
                if (obj.Object != null)
                {
                    if (!obj.PosFromObject)
                    {
                        Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                        obj.Object.SetLocalTransform((kv.Value.RotationMatrix ?? Matrix4x4.Identity) *
                                                     Matrix4x4.CreateTranslation(transform));
                    }
                    Cutscene.World.AddObject(obj.Object);
                }
                obj.Entity = kv.Value;
                Objects[kv.Key] = obj;
            }
        }

        Queue<ThnEvent> delaySoundEvents = new Queue<ThnEvent>();
        public void Update(double delta)
        {
            if (CurrentTime > Duration) return;
            CurrentTime += delta;
            //Don't run sound on T=0 exactly to avoid desync
            while(delaySoundEvents.Count > 0 && CurrentTime > 0)
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
            for (int i = (processors.Count - 1); i >= 0; i--)
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
                if (v.Instance != null && !v.Instance.Disposed)
                {
                    v.Instance.Stop();
                    v.Instance.Dispose();
                    v.Instance = null;
                }
            }
            Sounds = new Dictionary<string, ThnSoundInstance>();
        }
    }
}