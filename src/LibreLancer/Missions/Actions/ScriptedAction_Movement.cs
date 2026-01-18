// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.World;

namespace LibreLancer.Missions.Actions
{
    public class Act_MovePlayer : ScriptedAction
    {
        public Vector3 Position;
        public float Unknown; //1 in M01A

        public Act_MovePlayer()
        {
        }
        public Act_MovePlayer(MissionAction act) : base(act)
        {
            GetVector3(nameof(Position), 0, out Position, act.Entry);
            if(act.Entry.Count > 3)
                Unknown = act.Entry[3].ToSingle();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_MovePlayer", Position.X, Position.Y, Position.Z, Unknown);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() => runtime.Player.Space.ForceMove(Position));
        }
    }

    public class Act_Cloak : ScriptedAction
    {
        public string Target = string.Empty;
        public bool Cloaked;

        public Act_Cloak()
        {
        }

        public Act_Cloak(MissionAction a) : base(a)
        {
            GetString(nameof(Target),  0, out Target, a.Entry);
            GetBoolean(nameof(Cloaked), 1, out Cloaked, a.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_Cloak", Target, Cloaked);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var obj = runtime.Player.Space.World.GameWorld.GetObject(Target);
                if (obj == null)
                {
                    return;
                }

                FLLog.Debug("Mission", $"{obj} change cloaked to {Cloaked}");
                if (Cloaked) obj.Flags |= GameObjectFlags.Cloaked;
                else obj.Flags &= ~GameObjectFlags.Cloaked;
            });
        }

    }

    public class Act_PobjIdle : ScriptedAction
    {
        public Act_PobjIdle()
        {
        }
        public Act_PobjIdle(MissionAction a) : base(a)
        {
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            // Required for M01A to function
            // Does cause explosions in some later missions
            runtime.Player.MissionWorldAction(() => runtime.Player.RpcClient.StopShip());
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_PobjIdle", "no_params");
        }
    }

    public class Act_SetInitialPlayerPos : ScriptedAction
    {
        public Vector3 Position;
        public Quaternion Orientation;

        public Act_SetInitialPlayerPos()
        {
        }

        public Act_SetInitialPlayerPos(MissionAction act) : base(act)
        {
            GetVector3(nameof(Position), 0, out Position, act.Entry);
            GetQuaternion(nameof(Quaternion), 3, out Orientation, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetInitialPlayerPos", Position.X, Position.Y, Position.Z, Orientation.W, Orientation.X, Orientation.Y, Orientation.Z);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
                runtime.Player.Space.ForceMove(Position, Orientation));
        }
    }

    public class Act_RelocateShip : ScriptedAction
    {
        public string Ship = string.Empty;
        public Vector3 Position;
        public Quaternion? Orientation;

        public Act_RelocateShip()
        {
        }

        public Act_RelocateShip(MissionAction act) : base(act)
        {
            GetString(nameof(Ship), 0, out Ship, act.Entry);
            GetVector3(nameof(Position), 1, out Position, act.Entry);
            if (act.Entry.Count > 4)
            {
                GetQuaternion(nameof(Orientation), 4, out var o, act.Entry);
                Orientation = o;
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> entry = [Ship, Position.X, Position.Y, Position.Z];

            if (Orientation is not null)
            {
                entry.Add(Orientation.Value.W);
                entry.Add(Orientation.Value.X);
                entry.Add(Orientation.Value.Y);
                entry.Add(Orientation.Value.Z);
            }

            section.Entry("Act_RelocateShip", entry.ToArray());
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
