using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;

namespace LibreLancer.World
{
    public class ShipFormation
    {
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

        public Vector3? PlayerPosition;

        public Vector3 GetShipOffset(GameObject self)
        {
            if (LeadShip == self) return Vector3.Zero;
            if (PlayerPosition != null && ((self.Flags & GameObjectFlags.Player) == GameObjectFlags.Player))
                return PlayerPosition.Value;
            var idx = _followers.IndexOf(self);
            if (idx == -1) throw new InvalidOperationException("Ship not in formation");
            return Offsets[idx];
        }

        public void SetShipOffset(GameObject self, Vector3 offset)
        {
            if (LeadShip == self) return;
            var idx = _followers.IndexOf(self);
            if (idx == -1) throw new InvalidOperationException("Ship not in formation");
            Offsets[idx] = offset;
        }

        public Vector3 GetShipPosition(GameObject self)
        {
            return Vector3.Transform(GetShipOffset(self), LeadShip.WorldTransform);
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
            UpdatePlayers();
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
            if(obj.TryGetComponent<SPlayerComponent>(out var player))
                player.Player.RpcClient.UpdateFormation(new NetFormation());
            UpdatePlayers();
        }

        void UpdatePlayers()
        {
            if(LeadShip.TryGetComponent<SPlayerComponent>(out var player))
                player.Player.RpcClient.UpdateFormation(ToNetFormation(LeadShip));
            foreach (var f in _followers) {
                if(f.TryGetComponent<SPlayerComponent>(out player))
                    player.Player.RpcClient.UpdateFormation(ToNetFormation(f));
            }
        }

        public ShipFormation()
        {
        }

        public ShipFormation(GameObject lead, FormationDef formation)
        {
            LeadShip = lead;
            _followers = new List<GameObject>();
            UpdatePlayers();
            Offsets = formation.Positions.Skip(1).ToArray();

        }
        public ShipFormation(GameObject lead, params GameObject[] follow)
        {
            LeadShip = lead;
            _followers = new List<GameObject>(follow);
            UpdatePlayers();
        }

        static int GetId(GameObject obj, GameObject self)
        {
            if (obj == self) return 0;
            return obj.NetID;
        }

        NetFormation ToNetFormation(GameObject self)
        {
            var nf = new NetFormation();
            nf.Exists = true;
            nf.LeadShip = GetId(LeadShip, self);
            nf.Followers = Followers.Select(x => GetId(x, self)).ToArray();
            return nf;
        }
    }
}
