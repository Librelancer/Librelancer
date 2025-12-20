using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;

namespace LibreLancer.World
{
    public class ShipFormation
    {
        static uint _counter;

        public uint ID { get; } = Interlocked.Increment(ref _counter);

        public uint Version { get; private set; } = 1;

        public ulong Hash => (ulong)Version | (((ulong)ID) << 32);

        public GameObject LeadShip { get; private set; }
        public IReadOnlyList<GameObject> Followers => _followers;

        List<GameObject> _followers = new List<GameObject>();

        public Vector3[] Offsets = new[]
        {
            new Vector3(-60, 0, 0),
            new Vector3(60, 0, 0),
            new Vector3(0, -60, 0),
            new Vector3(0, 60, 0)
        };



        static Vector3[] defaultOffsets = new[]
        {
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0)
        };

        static Vector3 DefaultOffset(int i)
        {
            var dir = defaultOffsets[i % 4];
            var len = 60 + (i / 4) * 20;
            return dir * len;
        }

        public Vector3? PlayerPosition;

        public Vector3 GetShipOffset(GameObject self)
        {
            if (LeadShip == self) return Vector3.Zero;
            if (PlayerPosition != null && ((self.Flags & GameObjectFlags.Player) == GameObjectFlags.Player))
                return PlayerPosition.Value;
            var idx = _followers.IndexOf(self);
            if (idx == -1) throw new InvalidOperationException("Ship not in formation");
            if (idx >= Offsets.Length)
                return DefaultOffset(idx);
            return Offsets[idx];
        }

        public void SetShipOffset(GameObject self, Vector3 offset)
        {
            if (LeadShip == self) return;
            var idx = _followers.IndexOf(self);
            if (idx == -1) throw new InvalidOperationException("Ship not in formation");
            Offsets[idx] = offset;
        }

        public Vector3 GetShipPosition(GameObject self, bool isPlayer = false)
        {
            var offset = isPlayer
                ? (PlayerPosition ?? GetShipOffset(self))
                : GetShipOffset(self);
            return LeadShip.WorldTransform.Transform(offset);
        }

        public bool Contains(GameObject obj)
        {
            return LeadShip == obj || _followers.Contains(obj);
        }

        public void Add(GameObject obj)
        {
            _followers.Add(obj);
            //Sort player to end (SP formations)
            if (PlayerPosition != null)
            {
                var pobj = _followers.FirstOrDefault(x => (x.Flags & GameObjectFlags.Player) == GameObjectFlags.Player);
                if (pobj != null)
                {
                    _followers.Remove(pobj);
                    _followers.Add(pobj);
                }
            }
            Version++;
        }

        public void Remove(GameObject obj)
        {
            if (LeadShip == obj) {
                if (Followers.Count > 0)
                {
                    LeadShip = Followers[0];
                    _followers.RemoveAt(0);
                }
            }
            else
            {
                if (!_followers.Remove(obj))
                    throw new InvalidOperationException("Ship not in formation");
            }
            obj.Formation = null;
            Version++;
        }

        public ShipFormation()
        {
        }

        public ShipFormation(GameObject lead, FormationDef formation)
        {
            LeadShip = lead;
            _followers = new List<GameObject>();
            Offsets = formation.Positions.Skip(1).ToArray();
            PlayerPosition = formation.PlayerPosition;
        }

        public ShipFormation(GameObject lead, params GameObject[] follow)
        {
            LeadShip = lead;
            _followers = new List<GameObject>(follow);
        }

        static int GetId(GameObject obj, GameObject self)
        {
            if (obj == self) return 0;
            return obj.NetID;
        }

        public NetFormation ToNetFormation(GameObject self)
        {
            var nf = new NetFormation();
            nf.Exists = true;
            nf.LeadShip = GetId(LeadShip, self);
            nf.Followers = Followers.Select(x => GetId(x, self)).ToArray();
            nf.YourPosition = PlayerPosition ?? GetShipOffset(self);
            return nf;
        }

        public override string ToString()
        {
            return $"Lead: {LeadShip}\n" + string.Join("\n", _followers.Select(x => $"{x}: {GetShipOffset(x)}"));
        }
    }
}
