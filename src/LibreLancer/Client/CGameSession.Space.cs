using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Interface;
using LibreLancer.Missions;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Server;
using LibreLancer.Sounds.VoiceLines;
using LibreLancer.Thn;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client;

public partial class CGameSession
{
    private struct PlayerMoveState
    {
        public uint Tick;
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 Steering;
        public Vector3 AimPoint;
        public float Throttle;
        public StrafeControls Strafe;
        public bool Thrust;
        public bool CruiseEnabled;
        public ProjectileFireCommand? FireCommand;
    }

    public UpdateAck Acks;
    public double AdjustedInterval = 1.0;
    public int LastTickOffset;
    public int UpdateQueueCount => updatePackets.Count;
    public int AverageTickOffset => ticks.Average;
    public CircularBuffer<int> UpdatePacketSizes = new(200);
    public int DroppedInputs;


    private readonly MovingAverage<int> ticks = new(90);

    private CrcIdMap[] crcMap = [];
    private int jumpTimer;
    private CircularBuffer<PlayerMoveState> moveState = new(128);
    private readonly CircularBuffer<SPUpdatePacket> oldPackets = new(1000);
    private readonly Queue<IPacket> updatePackets = new();

    private volatile bool processUpdatePackets;
    private int tickSyncCounter = 0;
    private double totalTimeForTick;

    #region Update Loop

    public void BeginUpdateProcess()
    {
        processUpdatePackets = true;
        moveState = new CircularBuffer<PlayerMoveState>(128);
    }

    private NetInputControls FromMoveState(int i)
    {
        i++;
        return new NetInputControls
        {
            Tick = moveState[^i].Tick,
            Steering = moveState[^i].Steering,
            AimPoint = moveState[^i].AimPoint,
            Strafe = moveState[^i].Strafe,
            Throttle = moveState[^i].Throttle,
            Cruise = moveState[^i].CruiseEnabled,
            Thrust = moveState[^i].Thrust,
            FireCommand = moveState[^i].FireCommand
        };
    }

    public void UpdateStart(SpaceGameplay gp)
    {
        var elapsed = (uint)((Game.TotalTime - totalTimeForTick) / (1 / 60.0f));
        FLLog.Info("Player", $"{elapsed} ticks elapsed after load");
        WorldTick += elapsed;
    }

    public void GameplayUpdate(SpaceGameplay gp, double delta)
    {
        WorldTick++;
        UpdateAudio();

        while (gameplayActions.TryDequeue(out var act))
            act();

        if (!paused)
        {
            var player = gp.player;
            var phys = player.GetComponent<ShipPhysicsComponent>()!;
            var steering = player.GetComponent<ShipSteeringComponent>()!;
            var wp = player.GetComponent<WeaponControlComponent>()!;
            moveState.Enqueue(new PlayerMoveState
            {
                Tick = WorldTick,
                Position = player.PhysicsComponent!.Body!.Position,
                Orientation = player.PhysicsComponent.Body.Orientation,
                Steering = steering.OutputSteering,
                AimPoint = wp.AimPoint,
                Strafe = phys.CurrentStrafe,
                Throttle = phys.EnginePower,
                Thrust = steering.Thrust,
                CruiseEnabled = steering.Cruise,
                FireCommand = gp.world.Projectiles!.GetQueuedRequest()
            });

            // Store multiple updates for redundancy.
            var ip = new InputUpdatePacket
            {
                Current = FromMoveState(0),
                Acks = Acks
            };

            if (gp.Selection.Selected != null)
                ip.SelectedObject = gp.Selection.Selected;

            if (moveState.Count > 1)
                ip.HistoryA = FromMoveState(1);

            if (moveState.Count > 2)
                ip.HistoryB = FromMoveState(2);

            if (moveState.Count > 3)
                ip.HistoryC = FromMoveState(3);

            connection.SendPacket(ip, PacketDeliveryMethod.SequenceA);

            if (processUpdatePackets)
            {
                List<SPUpdatePacket> toUpdate = [];

                while (updatePackets.TryDequeue(out var pkt))
                {
                    var sp = GetUpdatePacket(pkt);

                    if (sp != null)
                        toUpdate.Add(sp);
                }

                for (var i = 0; i < toUpdate.Count; i++)
                    // Only do resync on the last packet processed this frame
                    // Stops the resync spiral of death
                    ProcessUpdate(toUpdate[i], gp, i == toUpdate.Count - 1);

                if (toUpdate.Count > 0)
                    ClockSync(toUpdate[^1]);
            }
        }

        (connection as GameNetClient)?.Update(); // Send packets at 60fps
    }

    private void ClockSync(SPUpdatePacket packet)
    {
        var tickOffset = (int)(packet.InputSequence - (long)packet.Tick);
        LastTickOffset = tickOffset;
        jumpTimer--;

        if (jumpTimer < 0)
            jumpTimer = 0;

        if (tickOffset < -50 && jumpTimer == 0)
        {
            WorldTick += 32; // Jump ahead in time
            jumpTimer = 10;
            AdjustedInterval = 0.9687;
            return;
        }

        ticks.AddValue(tickOffset);

        if (tickOffset < 0)
        {
            ticks.ForceSetAverage(tickOffset);
            DroppedInputs++;
        }

        AdjustedInterval = ticks.Average switch
        {
            <= -16 => 0.75,
            <= -8 => 0.875,
            <= -4 => 0.92,
            < 0 => 0.9687,
            >= 16 => 1.205,
            >= 8 => 1.085,
            >= 6 => 1.0312,
            >= 4 => 1.0070,
            >= 3 when !Multiplayer => 1.0050,
            _ => 1.0
        };
    }

    private ObjectUpdate GetUpdate(uint tick, int id)
    {
        for (var i = 0; i < oldPackets.Count; i++)
        {
            if (oldPackets[i].Tick != tick)
                continue;

            foreach (var packet in oldPackets[i].Updates)
                if (packet.ID.Value == id)
                    return packet;

            throw new Exception($"History {tick} missing id {id}");
        }

        throw new Exception($"History {tick} missing");
    }

    private SPUpdatePacket? GetUpdatePacket(IPacket p)
    {
        if (p is SPUpdatePacket sp)
            return sp;

        var mp = (PackedUpdatePacket)p;
        var oldPlayerState = new PlayerAuthState();

        if (mp.OldTick != 0)
        {
            int i;

            for (i = 0; i < oldPackets.Count; i++)
            {
                if (oldPackets[i].Tick != mp.OldTick)
                    continue;

                oldPlayerState = oldPackets[i].PlayerState;
                break;
            }

            if (i == oldPackets.Count)
            {
                FLLog.Error("Net", $"Unable to find old tick {mp.OldTick}, resetting ack");
                Acks = default;
                return null;
            }
        }

        UpdatePacketSizes.Enqueue(mp.DataSize);

        var updates = mp.GetUpdates(oldPlayerState, GetUpdate);
        var nsp = new SPUpdatePacket
        {
            Tick = mp.Tick,
            InputSequence = mp.InputSequence,
            PlayerState = updates.AuthState,
            Updates = updates.Updates
        };


        oldPackets.Enqueue(nsp);
        // Create new acknowledgement history
        var prevAcks = Acks;
        Acks = new UpdateAck(mp.Tick, 0);

        for (uint i = 1; i < 64; i++)
        {
            var tick = mp.Tick - i;
            Acks[tick] = prevAcks[tick];
        }

        return nsp;
    }

    private void ProcessUpdate(SPUpdatePacket p, SpaceGameplay gp, bool resync)
    {
        foreach (var update in p.Updates)
            UpdateObject(update, gp.world);
        var hp = gp.player.GetComponent<CHealthComponent>();
        var state = p.PlayerState;

        if (hp != null)
        {
            hp.CurrentHealth = state.Health;
            var sh = gp.player.GetFirstChildComponent<CShieldComponent>();
            sh?.SetShieldHealth(state.Shield);
        }

        if (gp?.player == null || !resync)
            return;

        for (var i = moveState.Count - 1; i >= 0; i--)
        {
            if (moveState[i].Tick != p.Tick)
                continue;

            var errorPos = state.Position - moveState[i].Position;
            var errorQuat = MathHelper.QuatError(state.Orientation, moveState[i].Orientation);
            var phys = gp.player.GetComponent<ShipPhysicsComponent>()!;

            if (p.PlayerState.CruiseAccelPct > 0 || p.PlayerState.CruiseChargePct > 0)
            {
                phys.ResyncChargePercent(p.PlayerState.CruiseChargePct,
                    1 / 60.0f * (moveState.Count - i));
                phys.ResyncCruiseAccel(p.PlayerState.CruiseAccelPct, 1 / 60.0f * (moveState.Count - i));
            }

            if (errorPos.Length() > 0.1 || errorQuat > 0.1f)
            {
                // We now do a basic resim without collision
                // This needs some work to not show the errors in collision on screen
                // for the client, but it's almost there
                // This is much faster than stepping the entire simulation again
                FLLog.Info("Client",
                    $"Applying correction at tick {p.InputSequence}. Errors ({errorPos.Length()},{errorQuat})");
                var transform = gp.player.LocalTransform;
                var predictedPos = transform.Position;
                var predictedOrient = transform.Orientation;
                moveState[i].Position = state.Position;
                moveState[i].Orientation = state.Orientation;
                // Set states
                gp.player.SetLocalTransform(new Transform3D(state.Position, state.Orientation));
                gp.player.PhysicsComponent!.Body!.LinearVelocity = state.LinearVelocity;
                gp.player.PhysicsComponent.Body.AngularVelocity = state.AngularVelocity;
                phys.ChargePercent = state.CruiseChargePct;
                phys.CruiseAccelPct = state.CruiseAccelPct;

                // simulate inputs - only outside a tradelane. we go back in time for a tradelane a bit
                for (i = i + 1; i < moveState.Count; i++)
                    Resimulate(i, gp);

                SmoothError(gp.player, predictedPos, predictedOrient);
                gp.player.PhysicsComponent.Update(1 / 60.0, gp.world);
            }

            break;
        }
    }

    private void UpdateObject(ObjectUpdate update, GameWorld world)
    {
        var obj = world.GetObject(update.ID);

        if (obj == null)
            return;

        if (obj.TryGetComponent<CEngineComponent>(out var eng))
        {
            eng.Speed = update.Throttle;

            foreach (var comp in obj.GetChildComponents<CThrusterComponent>())
                comp.Enabled = update.CruiseThrust == CruiseThrustState.Thrusting;
        }

        if (obj.TryGetComponent<CHealthComponent>(out var health))
            health.CurrentHealth = update.HullValue;

        if (obj.TryGetFirstChildComponent<CShieldComponent>(out var sh))
            sh.SetShieldHealth(update.ShieldValue);

        if (obj.TryGetComponent<WeaponControlComponent>(out var weapons) && (update.Guns?.Length ?? 0) > 0)
        {
            if (weapons.NetOrderWeapons == null)
                weapons.UpdateNetWeapons();

            weapons.SetRotations(update.Guns!);
        }

        if (obj.SystemObject != null)
            return;

        var oldPos = obj.LocalTransform.Position;
        var oldQuat = obj.LocalTransform.Orientation;
        obj.PhysicsComponent!.Body.LinearVelocity = update.LinearVelocity.Vector;
        obj.PhysicsComponent.Body.AngularVelocity = update.AngularVelocity.Vector;
        obj.PhysicsComponent.Body.Activate();
        obj.PhysicsComponent.Body.SetTransform(new Transform3D(update.Position, update.Orientation.Quaternion));

        SmoothError(obj, oldPos, oldQuat);
    }

    private void Resimulate(int i, SpaceGameplay gameplay)
    {
        var physComponent = gameplay.player.GetComponent<ShipPhysicsComponent>();
        var player = gameplay.player;
        physComponent!.CurrentStrafe = moveState[i].Strafe;
        physComponent.EnginePower = moveState[i].Throttle;
        physComponent.Steering = moveState[i].Steering;
        physComponent.ThrustEnabled = moveState[i].Thrust;
        physComponent.Update(1 / 60.0f, gameplay.world);
        gameplay.player.PhysicsComponent!.Body!.PredictionStep(1 / 60.0f);
        moveState[i].Position = player.PhysicsComponent!.Body!.Position;
        moveState[i].Orientation = player.PhysicsComponent.Body.Orientation;
    }

    private void SmoothError(GameObject obj, Vector3 oldPos, Quaternion oldQuat)
    {
        var newPos = obj.PhysicsComponent!.Body!.Position;
        var newOrient = obj.PhysicsComponent.Body.Orientation;

        if ((oldPos - newPos).Length() >
            obj.PhysicsComponent.Body.LinearVelocity.Length() * 0.33f)
        {
            obj.PhysicsComponent.PredictionErrorPos = Vector3.Zero;
            obj.PhysicsComponent.PredictionErrorQuat = Quaternion.Identity;
        }
        else
        {
            obj.PhysicsComponent.PredictionErrorPos = oldPos - newPos;
            obj.PhysicsComponent.PredictionErrorQuat =
                Quaternion.Inverse(newOrient) * oldQuat;
        }
    }

    #endregion

    #region Despawning

    void IClientPlayer.DestroyMissile(int id, bool explode)
    {
        RunSync(() =>
        {
            var despawn = spaceGameplay!.world.GetNetObject(id);

            if (despawn != null)
            {
                if (explode && despawn.TryGetComponent<CMissileComponent>(out var ms)
                            && ms.Missile?.ExplodeFx != null)
                    spaceGameplay.world.Renderer!.SpawnTempFx(ms.Missile.ExplodeFx.GetEffect(Game.ResourceManager),
                        despawn.LocalTransform.Position);

                despawn.Unregister(spaceGameplay.world);
                spaceGameplay.world.RemoveObject(despawn);
                FLLog.Debug("Client", $"Destroyed missile {id}");
            }
        });
    }

    #endregion

    void IClientPlayer.UndockFrom(ObjNetId netId, int index)
    {
        RunSync(() =>
        {
            var obj = spaceGameplay!.world.GetObject(netId);

            if (obj == null)
                return;

            if (obj.TryGetComponent<DockInfoComponent>(out var dock))
                spaceGameplay.SetDockCam(dock.GetDockCamera(index)!);

            spaceGameplay.pilotComponent!.Undock(obj, index);
        });
    }

    void IClientPlayer.RunDirectives(MissionDirective[] directives)
    {
        FLLog.Debug("Client", "Received directives for player");
        RunSync(() => { spaceGameplay!.Directives.SetDirectives(directives, spaceGameplay.world); });
    }

    void IClientPlayer.UpdateAnimations(ObjNetId id, NetCmpAnimation[] animations)
    {
        RunSync(() => spaceGameplay!.world.GetObject(id)?.AnimationComponent?.UpdateAnimations(animations));
    }

    void IClientPlayer.CallThorn(string? thorn, ObjNetId mainObject)
    {
        RunSync(() =>
        {
            if (thorn == null)
            {
                spaceGameplay?.Thn = null;
            }
            else
            {
                var thn = new ThnScript(Game.GameData.VFS.ReadAllBytes(Game.GameData.Items.DataPath(thorn)!),
                    Game.GameData.Items.ThornReadCallback, thorn);
                var mo = spaceGameplay?.world.GetObject(mainObject);

                if (mo != null)
                {
                    FLLog.Info("Client", "Found thorn mainObject");
                }
                else
                {
                    FLLog.Info("Client", $"Did not find mainObject with ID `{mainObject}. Assume player`");
                    mo = spaceGameplay!.player;
                }

                spaceGameplay!.Thn = new Cutscene(new ThnScriptContext(null) { MainObject = mo }, spaceGameplay);
                spaceGameplay.Thn.BeginScene(thn);
            }
        });
    }

    void IClientPlayer.UpdateAttitude(ObjNetId id, RepAttitude attitude)
    {
        RunSync(() =>
        {
            var obj = spaceGameplay!.world.GetObject(id);

            if (obj != null)
            {
                obj.Flags &= ~GameObjectFlags.Reputations;

                if (attitude == RepAttitude.Friendly)
                    obj.Flags |= GameObjectFlags.Friendly;

                if (attitude == RepAttitude.Neutral)
                    obj.Flags |= GameObjectFlags.Neutral;

                if (attitude == RepAttitude.Hostile)
                    obj.Flags |= GameObjectFlags.Hostile;
            }
        });
    }

    void IClientPlayer.DespawnObject(int id, bool explode)
    {
        RunSync(() =>
        {
            var despawn = spaceGameplay!.world.GetNetObject(id);

            if (despawn != null)
            {
                if (explode)
                    spaceGameplay.Explode(despawn);

                despawn.Unregister(spaceGameplay.world);
                spaceGameplay.world.RemoveObject(despawn);
                FLLog.Debug("Client", $"Despawned {id}");
            }
            else
            {
                FLLog.Warning("Client", $"Tried to despawn unknown {id}");
            }
        });
    }

    void IClientPlayer.PlaySound(string sound)
    {
        audioActions.Enqueue(() => Game.Sound.PlayOneShot(sound));
    }

    void IClientPlayer.DestroyPart(ObjNetId id, uint part)
    {
        RunSync(() =>
        {
            spaceGameplay!.world.GetObject(id)
                ?.DisableCmpPart(part, spaceGameplay!.world, Game.ResourceManager, out _);
        });
    }

    void IClientPlayer.RunMissionDialog(NetDlgLine[] lines)
    {
        RunSync(() => { RunDialog(lines); });
    }

    void IClientPlayer.StopShip()
    {
        FLLog.Debug("Mission", "StopShip() call received");
        RunSync(() => spaceGameplay!.StopShip());
    }

    void IClientPlayer.MarkImportant(int id, bool important)
    {
        RunSync(() =>
        {
            var o = spaceGameplay!.world.GetNetObject(id);

            if (o == null)
            {
                FLLog.Warning("Client", $"Could not find obj {id} to mark as important");
            }
            else
            {
                if (important)
                    o.Flags |= GameObjectFlags.Important;
                else
                    o.Flags &= ~GameObjectFlags.Important;
            }
        });
    }

    void IClientPlayer.PlayMusic(string? music, float fade)
    {
        audioActions.Enqueue(() =>
        {
            if (string.IsNullOrWhiteSpace(music) ||
                music.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                Game.Sound.StopMusic(fade);
            }
            else
            {
                spaceGameplay?.RtcMusic = true;
                Game.Sound.PlayMusic(music, fade);
            }
        });
    }

    void IClientPlayer.UpdateEffects(ObjNetId id, SpawnedEffect[] effect)
    {
        RunSync(() =>
        {
            var obj = spaceGameplay!.world.GetObject(id);

            if (obj != null)
            {
                if (!obj.TryGetComponent<CNetEffectsComponent>(out var fx))
                {
                    fx = new CNetEffectsComponent(obj);
                    obj.AddComponent(fx);
                }

                fx.UpdateEffects(effect, spaceGameplay!.world);
            }
        });
    }

    void IClientPlayer.TradelaneDisrupted()
    {
        inTradelane = false;
        RunSync(spaceGameplay!.TradelaneDisrupted);
    }

    void IClientPlayer.EndTradelane()
    {
        inTradelane = false;
        RunSync(spaceGameplay!.EndTradelane);
    }

    void IClientPlayer.StartTractor(ObjNetId ship, ObjNetId target)
    {
        RunSync(() =>
        {
            var src = spaceGameplay!.world.GetObject(ship);
            var dst = spaceGameplay.world.GetObject(target);

            if (src != null &&
                dst != null &&
                src.TryGetComponent<CTractorComponent>(out var tractor))
                tractor.AddBeam(dst);
        });
    }

    void IClientPlayer.EndTractor(ObjNetId ship, ObjNetId target)
    {
        RunSync(() =>
        {
            var src = spaceGameplay!.world.GetObject(ship);
            var dst = spaceGameplay.world.GetObject(target);

            if (src != null &&
                dst != null &&
                src.TryGetComponent<CTractorComponent>(out var tractor))
                tractor.RemoveBeam(dst);
        });
    }

    void IClientPlayer.Cloak(ObjNetId ship)
    {
        RunSync(() =>
            spaceGameplay!.world.GetObject(ship)?.GetComponent<CloakComponent>()?.Cloak(spaceGameplay!.world));
    }

    void IClientPlayer.Uncloak(ObjNetId ship)
    {
        RunSync(() =>
            spaceGameplay!.world.GetObject(ship)?.GetComponent<CloakComponent>()?.Uncloak(spaceGameplay!.world));
    }

    void IClientPlayer.TractorFailed()
    {
        // empty
        Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.TractorFailure);
    }

    void IClientPlayer.UpdateLootObject(ObjNetId id, NetBasicCargo[] cargo)
    {
        var newCargo = cargo.Select(x
                => new BasicCargo(Game.GameData.Items.Equipment.Get(x.EquipCRC)!, x.Count))
            .Where(x => x.Item != null)
            .ToList();
        RunSync(() =>
        {
            var loot = spaceGameplay!.world.GetObject(id)!;

            if (loot.TryGetComponent<LootComponent>(out var l))
                l.Cargo = newCargo;
        });
    }

    void IClientPlayer.Killed()
    {
        RunSync(() => { spaceGameplay?.Killed(); });
    }

    void IClientPlayer.TradelaneActivate(uint id, bool left)
    {
        gameplayActions.Enqueue(() =>
        {
            if (!(spaceGameplay!.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false))
                return;

            if (left)
                tl.ActivateLeft();
            else
                tl.ActivateRight();
        });
    }

    void IClientPlayer.TradelaneDeactivate(uint id, bool left)
    {
        gameplayActions.Enqueue(() =>
        {
            if (!(spaceGameplay!.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false))
                return;

            if (left)
                tl.DeactivateLeft();
            else
                tl.DeactivateRight();
        });
    }

    void IClientPlayer.ClearScan()
    {
        scanLoadout = null;
        scanId = null;
        scannedInventory = [];
        gameplayActions.Enqueue(() => spaceGameplay!.ClearScan());
    }

    void IClientPlayer.UpdateScan(ObjNetId id, NetLoadoutDiff diff)
    {
        scanLoadout ??= new NetLoadout();
        scanId = id;
        scanLoadout = diff.Apply(scanLoadout);
        scannedInventory = BuildScanList(scanLoadout);
        gameplayActions.Enqueue(() => { spaceGameplay?.UpdateScan(); });
    }

    void IClientPlayer.StoryMissionFailed(int failedIds)
    {
        RunSync(() =>
        {
            spaceGameplay!.StoryFail(failedIds);
            Pause();
        });
    }

    void IClientPlayer.UpdateFormation(NetFormation form)
    {
        if (spaceGameplay?.pilotComponent is null)
            return;

        gameplayActions.Enqueue(() =>
        {
            if (!form.Exists)
            {
                FLLog.Debug("Client", "Formation cleared");
                spaceGameplay.player.Formation = null;

                if (spaceGameplay.pilotComponent.CurrentBehavior == AutopilotBehaviors.Formation)
                    spaceGameplay.pilotComponent.Cancel();
            }
            else
            {
                FLLog.Debug("Client", "Formation received");
                spaceGameplay.player.Formation = new ShipFormation(
                    ObjOrPlayer(form.LeadShip),
                    form.Followers.Select(ObjOrPlayer).ToArray()
                )
                {
                    PlayerPosition = form.YourPosition
                };

                FLLog.Debug("Client", $"Formation offset {form.YourPosition}");

                if (spaceGameplay.player.Formation.LeadShip != spaceGameplay.player)
                {
                    FLLog.Debug("Client", "Starting follow");
                    spaceGameplay.pilotComponent.StartFormation();
                }
            }
        });
    }

    void IClientPlayer.UpdateAllowedDocking(AllowedDocking docking)
    {
        allowedDocking = docking;
    }

    public void WorldReady()
    {
        spaceGameplay!.world.SetCrcTranslation(crcMap);

        while (gameplayActions.TryDequeue(out var act))
            act();
    }

    private void RunDialog(NetDlgLine[] lines, int index = 0)
    {
        if (index >= lines.Length)
            return;

        if (lines[index].TargetIsPlayer)
        {
            var obj = spaceGameplay!.world.GetObject(new ObjNetId(lines[index].Source));

            if (obj != null)
                spaceGameplay.OpenComm(obj, lines[index].Voice!);
        }

        Game.Sound.PlayVoiceLine(lines[index].Voice!, lines[index].Hash, () =>
        {
            RunSync(() =>
            {
                RpcServer.LineSpoken(lines[index].Hash);

                if (lines[index].TargetIsPlayer)
                    spaceGameplay!.ClearComm();

                RunDialog(lines, index + 1);
            });
        });
    }

    public static UIInventoryItem FromNetCargo(NetCargo item)
    {
        return new UIInventoryItem
        {
            ID = item.ID,
            Count = item.Count,
            Icon = item.Equipment!.Good!.Ini.ItemIcon,
            Good = item.Equipment.Good.Ini.Nickname,
            IdsInfo = item.Equipment.IdsInfo,
            IdsName = item.Equipment.IdsName,
            Volume = item.Equipment.Volume,
            Combinable = item.Equipment.Good.Ini.Combinable,
            CanMount = false,
            Equipment = item.Equipment,
            Hardpoint = item.Hardpoint
        };
    }

    private UIInventoryItem[] BuildScanList(NetLoadout loadout)
    {
        var list = loadout.Items
            .Select(ResolveCargo)
            .Where(x => x.Equipment?.Good != null)
            .Select(FromNetCargo)
            .ToList();

        Trader.SortGoods(this, list);
        return list.ToArray();
    }

    public UIInventoryItem[] GetScannedInventory(string filter)
    {
        var predicate = Trader.GetFilter(filter);
        return scannedInventory.Where(x => predicate(x.Equipment!)).ToArray();
    }

    private GameObject ObjOrPlayer(int id)
    {
        if (id == 0)
            return spaceGameplay!.player;

        return spaceGameplay!.world.GetNetObject(id)!;
    }

    public bool DockAllowed(GameObject gameObject)
    {
        if (allowedDocking == null)
            return true;

        if (!allowedDocking.CanTl)
        {
            if (allowedDocking.TlExceptions.Contains(gameObject.NicknameCRC))
                return true;

            if (gameObject.TryGetComponent<DockInfoComponent>(out var dockInfo) &&
                dockInfo.Action.Kind == DockKinds.Tradelane)
                return false;
        }

        if (allowedDocking.CanDock)
            return true;

        if (allowedDocking.DockExceptions.Contains(gameObject.NicknameCRC))
            return true;

        return !gameObject.TryGetComponent<DockInfoComponent>(out var di) || di.Action.Kind == DockKinds.Tradelane;
    }

    #region Spawning

    void IClientPlayer.SpawnObjects(ObjectSpawnInfo[] objects)
    {
        RunSync(() =>
        {
            foreach (var objInfo in objects)
            {
                GameObject? newObj;

                if ((objInfo.Flags & ObjectSpawnFlags.Debris) == ObjectSpawnFlags.Debris)
                {
                    newObj = CreateDebris(objInfo);
                }
                else if ((objInfo.Flags & ObjectSpawnFlags.Solar) == ObjectSpawnFlags.Solar)
                {
                    var solar = Game.GameData.Items.Archetypes.Get(objInfo.Loadout.ArchetypeCrc)!;
                    newObj = new GameObject(solar, null, Game.ResourceManager);

                    if (objInfo.Dock != null && solar.DockSpheres.Count > 0)
                        newObj.AddComponent(new DockInfoComponent(newObj)
                        {
                            Action = objInfo.Dock,
                            Spheres = solar.DockSpheres.ToArray()
                        });

                    if (solar.Hitpoints > 0)
                        newObj.AddComponent(new CHealthComponent(newObj)
                            { CurrentHealth = objInfo.Loadout.Health, MaxHealth = solar.Hitpoints });
                }
                else if ((objInfo.Flags & ObjectSpawnFlags.Loot) == ObjectSpawnFlags.Loot)
                {
                    var crate =
                        (LootCrateEquipment)Game.GameData.Items.Equipment.Get(objInfo.Loadout.ArchetypeCrc)!;
                    var model = crate.ModelFile!.LoadFile(Game.ResourceManager)!;
                    newObj = new GameObject(model, Game.ResourceManager)
                    {
                        Kind = GameObjectKind.Loot,
                        PhysicsComponent =
                        {
                            Mass = crate.Mass
                        },
                        ArchetypeName = crate.Nickname
                    };
                    newObj.AddComponent(new CHealthComponent(newObj)
                        { MaxHealth = crate.Hitpoints, CurrentHealth = crate.Hitpoints });
                    newObj.Name = new LootName(newObj);
                }
                else
                {
                    var shp = Game.GameData.Items.Ships.Get((int)objInfo.Loadout.ArchetypeCrc)!;
                    newObj = new GameObject(shp, Game.ResourceManager, true, true);
                    newObj.AddComponent(new CHealthComponent(newObj)
                        { CurrentHealth = objInfo.Loadout.Health, MaxHealth = shp.Hitpoints });
                    newObj.AddComponent(new CExplosionComponent(newObj, shp.Explosion!));
                }

                if (newObj is null)
                {
                    FLLog.Warning("Client", "Unable to spawn new object");
                    return;
                }

                // disable parts
                foreach (var p in objInfo.DestroyedParts)
                    newObj.DisableCmpPart(p, spaceGameplay.world, Game.ResourceManager, out _);

                newObj.Name ??= objInfo.Name;
                newObj.NetID = objInfo.ID.Value;
                newObj.Nickname = objInfo.Nickname;
                newObj.SetLocalTransform(new Transform3D(objInfo.Position, objInfo.Orientation));
                var head = Game.GameData.Items.Bodyparts.Get(objInfo.CommHead);
                var body = Game.GameData.Items.Bodyparts.Get(objInfo.CommBody);
                var helmet = Game.GameData.Items.Accessories.Get(objInfo.CommHelmet);

                if (head != null || body != null)
                    newObj.AddComponent(new CostumeComponent(newObj)
                    {
                        Head = head,
                        Body = body,
                        Helmet = helmet
                    });

                var fac = Game.GameData.Items.Factions.Get(objInfo.Affiliation);

                if (fac != null)
                    newObj.AddComponent(new CFactionComponent(newObj, fac));

                if ((objInfo.Flags & ObjectSpawnFlags.Friendly) == ObjectSpawnFlags.Friendly)
                    newObj.Flags |= GameObjectFlags.Friendly;

                if ((objInfo.Flags & ObjectSpawnFlags.Hostile) == ObjectSpawnFlags.Hostile)
                    newObj.Flags |= GameObjectFlags.Hostile;

                if ((objInfo.Flags & ObjectSpawnFlags.Neutral) == ObjectSpawnFlags.Neutral)
                    newObj.Flags |= GameObjectFlags.Neutral;

                if ((objInfo.Flags & ObjectSpawnFlags.Important) == ObjectSpawnFlags.Important)
                    newObj.Flags |= GameObjectFlags.Important;

                foreach (var eq in objInfo.Loadout.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                {
                    var equip = Game.GameData.Items.Equipment.Get(eq.EquipCRC);

                    if (equip == null)
                        continue;

                    EquipmentObjectManager.InstantiateEquipment(newObj, Game.ResourceManager, Game.Sound,
                        EquipmentType.LocalPlayer, eq.Hardpoint, equip);
                }

                if (newObj.Kind == GameObjectKind.Loot)
                {
                    var lt = new LootComponent(newObj);
                    newObj.AddComponent(lt);

                    foreach (var eq in objInfo.Loadout.Items.Where(x => string.IsNullOrWhiteSpace(x.Hardpoint)))
                    {
                        var equip = Game.GameData.Items.Equipment.Get(eq.EquipCRC);

                        if (equip == null)
                            continue;

                        lt.Cargo.Add(new BasicCargo(equip, eq.Count));
                    }
                }

                spaceGameplay!.world.AddObject(newObj);
                newObj.Register(spaceGameplay.world);

                if ((objInfo.Flags & ObjectSpawnFlags.Debris) == ObjectSpawnFlags.Debris ||
                    (objInfo.Flags & ObjectSpawnFlags.Loot) == ObjectSpawnFlags.Loot)
                    newObj.PhysicsComponent!.Body.SetDamping(0.5f, 0.2f);
                else
                    newObj.AddComponent(new WeaponControlComponent(newObj));

                if ((objInfo.Flags & ObjectSpawnFlags.Hidden) == ObjectSpawnFlags.Hidden &&
                    newObj.TryGetComponent<CloakComponent>(out var cloaked))
                    cloaked.SetInitCloaked();

                // add fx
                if (objInfo.Effects is { Length: > 0 })
                {
                    var fx = new CNetEffectsComponent(newObj);
                    newObj.AddComponent(fx);
                    fx.UpdateEffects(objInfo.Effects, spaceGameplay!.world);
                }

                FLLog.Debug("Client", $"Spawned {newObj.NetID}");
            }
        });
    }

    private GameObject? CreateDebris(ObjectSpawnInfo obj)
    {
        ModelResource? src;
        List<SeparablePart>? sep;
        float[]? lodranges;

        if ((obj.Flags & ObjectSpawnFlags.Solar) == ObjectSpawnFlags.Solar)
        {
            var solar = Game.GameData.Items.Archetypes.Get(obj.Loadout.ArchetypeCrc);
            sep = solar?.SeparableParts;
            src = solar?.ModelFile?.LoadFile(Game.ResourceManager);
            lodranges = solar?.LODRanges;
        }
        else
        {
            var ship = Game.GameData.Items.Ships.Get(obj.Loadout.ArchetypeCrc);
            sep = ship?.SeparableParts;
            src = ship?.ModelFile?.LoadFile(Game.ResourceManager);
            lodranges = ship?.LODRanges;
        }

        if (src is null || sep is null || lodranges is null)
            return null;

#pragma warning disable CS8670

        var collider = src.Collision;
        var mdl = ((IRigidModelFile)src.Drawable)?.CreateRigidModel(true, Game.ResourceManager);
        var newmodel = mdl!.Parts![obj.DebrisPart].CloneAsRoot(mdl);
        var partName = newmodel.Root.Name!;
        var sepInfo = sep.FirstOrDefault(x => x.Part.Equals(partName, StringComparison.OrdinalIgnoreCase));
        var go = new GameObject(newmodel, collider, partName, sepInfo?.Mass ?? 1, true)
        {
            Kind = GameObjectKind.Debris,
            Model =
            {
                SeparableParts = sep
            }
        };

#pragma warning restore CS8670

        if (go.RenderComponent is ModelRenderer mr)
            mr.LODRanges = lodranges;

        // Child damage cap
        if (sepInfo is { ChildDamageCap: not null } &&
            go.Model.TryGetHardpoint(sepInfo.ChildDamageCapHardpoint, out var capHp))
        {
            var dcap = GameObject.WithModel(sepInfo.ChildDamageCap.Model!, true, Game.ResourceManager);
            dcap.Attachment = capHp;
            dcap.Parent = go;
            dcap.RenderComponent!.InheritCull = false;

            if (dcap.Model!.TryGetHardpoint("DpConnect", out var dpConnect))
                dcap.SetLocalTransform(dpConnect.Transform.Inverse());

            go.Children.Add(dcap);
        }

        return go;
    }

    void IClientPlayer.SpawnPlayer(int ID, string system, CrcIdMap[] crcMap, NetObjective objective,
        Vector3 position, Quaternion orientation, uint tick)
    {
        enterCount++;
        PlayerNetID = ID;
        PlayerBase = null;
        CurrentObjective = objective;
        FLLog.Info("Client", $"Spawning in {system}");
        PlayerSystem = system;
        PlayerPosition = position;
        PlayerOrientation = orientation;
        SceneChangeRequired();
        var delay = connection.EstimateTickDelay();
        FLLog.Info("Player", $"Spawning at {tick} + delay {delay}");
        WorldTick = tick + connection.EstimateTickDelay();
        totalTimeForTick = Game.TotalTime;
        this.crcMap = crcMap;
    }

    void IClientPlayer.SpawnMissile(int id, bool playSound, uint equip, Vector3 position, Quaternion orientation)
    {
        RunSync(() =>
        {
            var eq = Game.GameData.Items.Equipment.Get(equip);

            if (eq is not MissileEquip mn)
                return;

            var go = new GameObject(mn.ModelFile!.LoadFile(Game.ResourceManager)!,
                Game.ResourceManager);
            go.SetLocalTransform(new Transform3D(position, orientation));
            go.NetID = id;
            go.Kind = GameObjectKind.Missile;
            go.PhysicsComponent?.Mass = 1;

            if (mn.Def.ConstEffect != null)
            {
                var fx = Game.GameData.Items.Effects.Get(mn.Def.ConstEffect)?
                    .GetEffect(Game.ResourceManager);
                var ren = new ParticleEffectRenderer(fx) { Attachment = go.GetHardpoint(mn.Def.HpTrailParent) };
                go.ExtraRenderers.Add(ren);
            }

            go.AddComponent(new CMissileComponent(go, mn));
            spaceGameplay!.world.AddObject(go);
            go.Register(spaceGameplay!.world);
        });
    }

    void IClientPlayer.SpawnProjectiles(ProjectileSpawn[] projectiles)
    {
        RunSync(() =>
        {
            foreach (var p in projectiles)
            {
                var owner = spaceGameplay!.world.GetObject(p.Owner);

                if (owner == spaceGameplay.player)
                    continue;

                if (owner != null && owner.TryGetComponent<WeaponControlComponent>(out var wc))
                {
                    var tgtUnique = 0;

                    if (wc.NetOrderWeapons == null)
                        wc.UpdateNetWeapons();

                    for (var i = 0; i < wc.NetOrderWeapons!.Length; i++)
                    {
                        if ((p.Guns & (1UL << i)) == 0)
                            continue;

                        var target = p.Target;

                        if ((p.Unique & (1UL << i)) != 0)
                            target = p.OtherTargets[tgtUnique++];

                        wc.NetOrderWeapons[i].Fire(target, spaceGameplay.world, null, true);
                    }
                }
            }
        });
    }

    #endregion
}
