
using System.Numerics;

namespace LibreLancer
{
    /// <summary>
    /// Class that handles receiving inputs from the network and applying them to the player
    /// Also stores a reference to the player class.
    ///
    /// Caveats:
    ///    - This will currently bug out if the network latency increases significantly
    ///      and stays increased. This only handles jitter and latency spikes, as well as
    ///      the occasional dropped packet.
    /// </summary>
    public class SPlayerComponent : GameComponent
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
        public int SequenceApplied = 0;

        public Player Player { get; private set; }

        public SPlayerComponent(Player player, GameObject parent) : base(parent)
        {
            this.Player = player;
        }

        public void Killed()
        {
            Player.Killed();
        }

        public void QueueInput(InputUpdatePacket input)
        {
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
                    inputs.Enqueue(faked);
                }
            }
            //Add on the current packet. LiteNetLib guarantees we always
            //receive packets in order, and will drop out of order packets.
            inputs.Enqueue(new ReceivedInputs(input.Current));
            //Recover lost inputs from redundant data sent in a packet
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
        
        bool GetInput(out NetInputControls packet, out int sequence)
        {
            packet = new NetInputControls();
            sequence = 0;
            if (_last == null && inputs.Count == 0)
            {
                //Haven't received any data yet
                return false;
            }
            if (inputs.Count > 0)
            {
                //Initialise buffer
                if (sequenceFetch == -int.MaxValue) {
                    sequenceFetch = inputs.Peek().Sequence - 7;
                }
                sequenceFetch++;
                if (inputs.Peek().Sequence < sequenceFetch) {
                    //Head of queue is behind what we are simulating
                    //This is where we should probably adapt to the average RTT
                    //getting much higher.
                    inputs.Dequeue();
                }
                if (inputs.Count > 0 && inputs.Peek().Sequence == sequenceFetch)
                {
                    var popInput = inputs.Dequeue();
                    if (inputs.Peek().FromClient) {
                        //We have valid input from the client
                        sequence = sequenceFetch;
                        _last = packet = popInput.Controls;
                    } else if( _last != null) {
                        //We have a lost packet, just use last frame's inputs
                        sequence = sequenceFetch;
                        packet = _last.Value;
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
    }
}