using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World.Components;

namespace LibreLancer.World
{
    public class ShipFormation
    {
        private static uint _counter;

        public uint ID { get; } = Interlocked.Increment(ref _counter);

        public uint Version { get; private set; } = 1;

        public ulong Hash => (ulong)Version | (((ulong)ID) << 32);

        public GameObject LeadShip { get; private set; } = null!;
        public IReadOnlyList<GameObject> Followers => _followers;

        private List<GameObject> _followers = [];

        public Vector3[] Offsets =
        [
            new Vector3(-60, 0, 0),
            new Vector3(60, 0, 0),
            new Vector3(0, -60, 0),
            new Vector3(0, 60, 0)
        ];

        private static Vector3[] defaultOffsets =
        [
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0)
        ];

        private static Vector3 DefaultOffset(int i)
        {
            var dir = defaultOffsets[i % 4];
            var len = 60 + (i / 4) * 20;
            return dir * len;
        }

        public Vector3? PlayerPosition;
        // The server sends the player's authored target because the client does not have every NPC offset.
        internal Vector3? PlayerTargetPosition;

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
            if (idx >= Offsets.Length)
            {
                var oldLength = Offsets.Length;
                Array.Resize(ref Offsets, idx + 1);
                for (int i = oldLength; i < Offsets.Length; i++)
                    Offsets[i] = DefaultOffset(i);
            }
            Offsets[idx] = offset;
            Version++;
        }

        public Vector3 GetShipPosition(GameObject self, bool isPlayer = false)
        {
            var offset = isPlayer && PlayerTargetPosition != null
                ? PlayerTargetPosition.Value
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
            // Sort player to end (SP formations)
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
            if (!Contains(obj))
                throw new InvalidOperationException("Ship not in formation");

            if (LeadShip == obj)
            {
                if (_followers.Count <= 1)
                {
                    Disband(obj);
                    return;
                }

                LeadShip = _followers[0];
                _followers.RemoveAt(0);
            }
            else
            {
                if (!_followers.Remove(obj))
                    throw new InvalidOperationException("Ship not in formation");

                if (_followers.Count == 0)
                {
                    Disband(obj);
                    return;
                }
            }

            obj.Formation = null;
            Version++;
        }

        private void Disband(GameObject removed)
        {
            LeadShip.Formation = null;
            foreach (var follower in _followers)
                follower.Formation = null;
            removed.Formation = null;
            _followers.Clear();
            Version++;
        }

        public ShipFormation()
        {
        }

        public ShipFormation(GameObject lead, FormationDef formation)
        {
            LeadShip = lead;
            _followers = [];
            Offsets = formation.Positions.Skip(1).ToArray();
            PlayerPosition = formation.PlayerPosition;
        }

        public ShipFormation(GameObject lead, params GameObject[] follow)
        {
            LeadShip = lead;
            _followers = new List<GameObject>(follow);
        }

        private static int GetId(GameObject obj, GameObject self)
        {
            if (obj == self) return 0;
            return obj.NetID;
        }

        public NetFormation ToNetFormation(GameObject self)
        {
            if (LeadShip != self &&
                ((LeadShip.Flags & GameObjectFlags.Exists) != GameObjectFlags.Exists || LeadShip.NetID == 0))
            {
                return new NetFormation();
            }

            var followers = Followers
                .Where(x => x == self || (x.Flags & GameObjectFlags.Exists) == GameObjectFlags.Exists && x.NetID != 0)
                .Select(x => GetId(x, self))
                .ToArray();
            var nf = new NetFormation
            {
                Exists = true,
                LeadShip = GetId(LeadShip, self),
                Followers = followers,
                YourPosition = GetShipOffset(self)
            };
            return nf;
        }

        public override string ToString()
        {
            return $"Lead: {LeadShip}\n" + string.Join("\n", _followers.Select(x => $"{x}: {GetShipOffset(x)}"));
        }
    }
}
