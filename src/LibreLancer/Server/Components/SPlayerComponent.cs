using System;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.Items;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
{
    /*
     * Component that handles a remote player controlling this GameObject
     * Stores a reference to the Player class, buffers input
     * and keeps track of cargo
     *
     * Notes:
     *   - When the buffer underflows (extended latency spike), the buffer is expanded and a hard
     *   resync of the tick numbers occurs, this causes a large resimulation on the client.
     */
    public class SPlayerComponent : AbstractCargoComponent
    {
        struct ReceivedInputs
        {
            public NetInputControls Controls;
            public int Sequence => Controls.Sequence;
            public bool FromClient;

            public ReceivedInputs(NetInputControls pkt)
            {
                Controls = pkt;
                FromClient = true;
            }
        }

        private CircularBuffer<ReceivedInputs> inputs = new (96);

        private CircularBuffer<(uint t, PlayerAuthState p, ObjectUpdate[] u)> oldStates =
            new CircularBuffer<(uint t, PlayerAuthState p, ObjectUpdate[] u)>(128);
        public uint MostRecentAck = 0;
        public int SequenceApplied = 0;

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

        public void QueueInput(InputUpdatePacket input)
        {
            MostRecentAck = input.AckTick;
            //Select object immediately
            if (input.SelectedIsCRC)
                SelectedObject = Parent.World.GetObject((uint) input.SelectedObject);
            else
                SelectedObject = Parent.World.GetFromNetID(input.SelectedObject);
            //We've lost some inputs along the way
            if (inputs.Count > 0 && inputs[^1].Sequence < input.Current.Sequence - 1)
            {
                //Insert invalid updates
                for (int i = inputs[^1].Sequence + 1; i < input.Current.Sequence; i++)
                {
                    var faked = new ReceivedInputs()
                    {
                        Controls = new NetInputControls() {Sequence = i}
                    };
                    faked.FromClient = false;
                    inputs.Enqueue(faked);
                }
            }
            //Add on the current packet. LiteNetLib guarantees we always
            //receive packets in order, and will drop out of order packets.
            inputs.Enqueue(new ReceivedInputs(input.Current));
            //Recover lost inputs from redundant data sent in the packet
            //This handles minor packet loss
            for (int i = inputs.Count - 1; i >= 0; i--)
            {
                if (inputs[i].Sequence == input.HistoryA.Sequence &&
                    !inputs[i].FromClient)
                {
                    inputs[i] = new ReceivedInputs(input.HistoryA);
                }

                if (inputs[i].Sequence == input.HistoryB.Sequence &&
                    !inputs[i].FromClient)
                {
                    inputs[i] = new ReceivedInputs(input.HistoryB);
                }

                if (inputs[i].Sequence == input.HistoryC.Sequence &&
                    !inputs[i].FromClient)
                {
                    inputs[i] = new ReceivedInputs(input.HistoryC);
                }
            }
        }

        private NetInputControls? _last;
        private int sequenceFetch = -int.MaxValue;
        private int underbufferCounter = 0;
        private bool fillAgain = false;

        private const int UNDERFLOW_THRESHOLD = 16;
        private const int NO_UNDERFLOW_THRESHOLD = 72;
        private int okFrames = 0;
        bool GetInput(out NetInputControls packet, out int sequence)
        {
            packet = new NetInputControls();
            sequence = 0;

            if (_last != null && fillAgain) {
                //We are waiting for the buffer to refill for our hard resync
                if (inputs.Count > 7) {
                    fillAgain = false;
                }
                else {
                    packet = _last.Value;
                    sequence = sequenceFetch;
                    return true;
                }
            }

            if (_last == null && inputs.Count == 0)
            {
                //Haven't received any data yet
                return false;
            }
            if (inputs.Count > 0)
            {
                //Initialise buffer
                if (sequenceFetch == -int.MaxValue) {
                    sequenceFetch = inputs.Peek().Sequence - 8;
                }
                sequenceFetch++;
                if (inputs.Peek().Sequence < sequenceFetch) {
                    //Head of queue is behind what we are simulating
                    //This is where we should probably adapt to the average RTT
                    //getting much higher.
                    okFrames = 0;
                    underbufferCounter++;
                    if (underbufferCounter > UNDERFLOW_THRESHOLD) {
                        FLLog.Info("Server", $"({Player.Name ?? Player.ID.ToString()}): Client input underflow detected, increasing buffer");
                        sequenceFetch--;
                        underbufferCounter = 0;
                        fillAgain = true;
                    }
                    inputs.Dequeue();
                }
                if (inputs.Count > 0 && inputs.Peek().Sequence == sequenceFetch)
                {
                    var popInput = inputs.Dequeue();
                    if (popInput.FromClient) {
                        //We have valid input from the client
                        sequence = sequenceFetch;
                        _last = packet = popInput.Controls;
                        okFrames++;
                        if (okFrames > NO_UNDERFLOW_THRESHOLD){
                            underbufferCounter = 0;
                            okFrames = 0;
                        }
                    } else if( _last != null) {
                        //We have a lost packet, just use last frame's inputs
                        sequence = sequenceFetch;
                        packet = _last.Value;
                        okFrames = 0;
                    }
                    else {
                        return false;
                    }
                    return true;
                }
                if (_last != null) {
                    sequence = sequenceFetch;
                    packet = _last.Value;
                    return true;
                } else {
                    //We have no data
                    return false;
                }
            }
            else if (_last != null) {
                //Queue empty, repeat last
                sequence = sequenceFetch++;
                packet = _last.Value;
                return true;
            }
            else {
                //We have no data
                return false;
            }
        }

        public override void Update(double time)
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var phys))
            {
                if (GetInput(out var input, out int sequence))
                {
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
                    }
                    phys.Tick = sequence;
                    SequenceApplied = sequence;
                }
            }
        }

        public override bool TryConsume(Equipment item)
        {
            var slot = Player.Character.Items.FirstOrDefault(x => x.Equipment == item);
            if (slot != null) {
                if(slot.Count <= 1)
                    Player.RemoteClient.DeleteSlot(slot.ID);
                else
                    Player.RemoteClient.UpdateSlotCount(slot.ID, slot.Count - 1);
                using var t = Player.Character.BeginTransaction();
                t.RemoveCargo(slot, 1);
                return true;
            }
            return false;
        }
    }
}
