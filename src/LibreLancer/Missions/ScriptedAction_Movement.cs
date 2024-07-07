// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.World;

namespace LibreLancer.Missions
{
    public class Act_MovePlayer : ScriptedAction
    {
        public Vector3 Position;
        public float Unknown; //1 in M01A

        public Act_MovePlayer(MissionAction act) : base(act)
        {
            Position = new Vector3(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(),
                act.Entry[2].ToSingle());
            if(act.Entry.Count > 3)
                Unknown = act.Entry[3].ToSingle();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() => runtime.Player.Space.ForceMove(Position));
        }
    }

    public class Act_Cloak : ScriptedAction
    {
        public string Target;
        public bool Cloaked;
        public Act_Cloak(MissionAction a) : base(a)
        {
            Target = a.Entry[0].ToString();
            Cloaked = a.Entry[1].ToBoolean();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var obj = runtime.Player.Space.World.GameWorld.GetObject(Target);
                if (obj != null)
                {
                    FLLog.Debug("Mission", $"{obj} change cloaked to {Cloaked}");
                    if (Cloaked) obj.Flags |= GameObjectFlags.Cloaked;
                    else obj.Flags &= ~GameObjectFlags.Cloaked;
                }
            });
        }

    }

    public class Act_PobjIdle : ScriptedAction
    {
        public Act_PobjIdle(MissionAction a) : base(a)
        {
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            //This is correct: but the NPCs currently blow you up when stopped
            //runtime.Player.RemoteClient.StopShip();
        }
    }

    public class Act_SetInitialPlayerPos : ScriptedAction
    {
        public Vector3 Position;
        public Quaternion Orientation;

        public Act_SetInitialPlayerPos(MissionAction act) : base(act)
        {
            Position = new Vector3(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(),
                act.Entry[2].ToSingle());
            Orientation = new Quaternion(act.Entry[4].ToSingle(), act.Entry[5].ToSingle(),
                act.Entry[6].ToSingle(), act.Entry[3].ToSingle());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
                runtime.Player.Space.ForceMove(Position, Orientation));
        }
    }

    public class Act_RelocateShip : ScriptedAction
    {
        public string Ship;
        public Vector3 Position;
        public Quaternion? Orientation;

        public Act_RelocateShip(MissionAction act) : base(act)
        {
            Ship = act.Entry[0].ToString();
            Position = new Vector3(act.Entry[1].ToSingle(), act.Entry[2].ToSingle(),
                act.Entry[3].ToSingle());
            if (act.Entry.Count > 4)
            {
                Orientation = new Quaternion(act.Entry[5].ToSingle(), act.Entry[6].ToSingle(), act.Entry[7].ToSingle(),
                    act.Entry[4].ToSingle());
            }
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Ship.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                runtime.Player.MissionWorldAction(() => runtime.Player.Space.ForceMove(Position, Orientation));
            }
            else if (script.Ships.ContainsKey(Ship))
            {
                runtime.Player.MissionWorldAction(() =>
                {
                    var obj = runtime.Player.Space.World.GameWorld.GetObject(Ship);
                    var quat = Orientation ?? obj.LocalTransform.Orientation;
                    obj.SetLocalTransform(new Transform3D(Position, quat));
                });
            }
        }
    }
}
