using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.Items;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
{
    /*
     * Component that handles a remote player controlling this GameObject
     * Stores a reference to the Player class, buffers input
     * and keeps track of cargo
     */
    public class SPlayerComponent : AbstractCargoComponent
    {
        private PriorityQueue<NetInputControls, uint> inputs = new();


        //Used for compressing delta info
        private CircularBuffer<(uint t, PlayerAuthState p, ObjectUpdate[] u)> oldStates =
            new CircularBuffer<(uint t, PlayerAuthState p, ObjectUpdate[] u)>(128);

        public uint MostRecentAck = 0;
        public Player Player { get; private set; }

        public GameObject SelectedObject { get; private set; }

        public SPlayerComponent(Player player, GameObject parent) : base(parent)
        {
            this.Player = player;
        }

        public void Killed()
        {
            Player.Killed();
        }

        public void GetAcknowledgedState(out uint ackTick, out PlayerAuthState authState, out ObjectUpdate[] updates)
        {
            ackTick = 0;
            authState = new PlayerAuthState();
            updates = Array.Empty<ObjectUpdate>();
            if (MostRecentAck == 0) return;
            for (int i = 0; i < oldStates.Count; i++) {
                if (oldStates[i].t == MostRecentAck)
                {
                    ackTick = MostRecentAck;
                    authState = oldStates[i].p;
                    updates = oldStates[i].u;
                    break;
                }
            }
        }

        public void EnqueueState(uint tick, PlayerAuthState auth, ObjectUpdate[] updates)
        {
            oldStates.Enqueue((tick, auth, updates));
        }

        public uint LatestReceived;

        public void QueueInput(InputUpdatePacket input)
        {
            MostRecentAck = input.AckTick;
            //Select object immediately
            SelectedObject = Parent.World.GetObject( input.SelectedObject);
            Enqueue(input.HistoryC);
            Enqueue(input.HistoryB);
            Enqueue(input.HistoryA);
            Enqueue(input.Current);
            LatestReceived = input.Current.Tick;
        }

        void Enqueue(NetInputControls controls)
        {
            if (controls.Tick == 0)
                return;
            if(controls.Tick >= GetCurrentTick())
                inputs.Enqueue(controls, controls.Tick);
        }

        uint GetCurrentTick() => Parent.World.Server.CurrentTick;


        bool GetInput(out NetInputControls packet)
        {
            NetInputControls currentTick = default;
            bool found = false;
            var tick = GetCurrentTick();
            while (inputs.TryDequeue(out var entry, out _)) {
                if (entry.Tick == tick) {
                    currentTick = entry;
                    found = true;
                } else if (entry.Tick > tick) {
                    inputs.Enqueue(entry, entry.Tick);
                    break;
                }
            }
            packet = currentTick;
            return found;
        }

        private ulong formationHash = 0;
        public override void Update(double time)
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var phys))
            {
                var wpc = Parent.GetComponent<WeaponControlComponent>();
                if (GetInput(out var input))
                {
                    wpc.AimPoint = input.AimPoint;
                    if (Player.InTradelane)
                    {
                        phys.Steering = Vector3.Zero;
                        phys.CurrentStrafe = StrafeControls.None;
                        phys.EnginePower = 0;
                        phys.ThrustEnabled = false;
                        phys.CruiseEnabled = false;
                    }
                    else
                    {
                        phys.Steering = input.Steering;
                        phys.CurrentStrafe = input.Strafe;
                        phys.EnginePower = input.Throttle;
                        phys.ThrustEnabled = input.Thrust;
                        phys.CruiseEnabled = input.Cruise;
                        if(input.FireCommand != null)
                            Parent.GetWorld().Server.FireProjectiles(input.FireCommand.Value, Player);
                    }
                }
            }
            if (Parent.Formation == null)
            {
                if(formationHash != 0)
                    Player.RpcClient.UpdateFormation(new NetFormation());
                formationHash = 0;
            }
            else if (formationHash != Parent.Formation.Hash)
            {
                formationHash = Parent.Formation.Hash;
                Player.RpcClient.UpdateFormation(Parent.Formation.ToNetFormation(Parent));
            }
        }

        public override int TryConsume(Equipment item, int maxCount = 1)
        {
            var slot = Player.Character.Items.FirstOrDefault(x => x.Equipment == item);
            if (slot != null)
            {
                var c = slot.Count;
                if(slot.Count <= maxCount)
                    Player.RpcClient.DeleteSlot(slot.ID);
                else
                    Player.RpcClient.UpdateSlotCount(slot.ID, slot.Count - maxCount);
                using var t = Player.Character.BeginTransaction();
                t.RemoveCargo(slot, 1);
                return c > maxCount ? maxCount : c;
            }
            return 0;
        }

        public override T FirstOf<T>()
        {
            var slot = Player.Character.Items.FirstOrDefault(x => x.Equipment is T);
            return (T)slot?.Equipment;
        }
    }
}
