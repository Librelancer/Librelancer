// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Utf.Anm;

namespace LibreLancer.World.Components
{
	public class AnimationComponent : GameComponent
    {
        private Dictionary<Script, ActiveAnimation> active = new();

		class ActiveAnimation
		{
			public string Name;
			public Script Script;
            public float StartT;
            public float ScriptDuration;
            public double WorldStartTime;
            public double Duration;
            public float TimeScale;
            public bool Reverse;
			public bool Loop;
            public int[] ObjectCursors;
            public int[] JointCursors;

            public ActiveAnimation(Script script)
            {
                ObjectCursors = new int[script.ObjectMaps.Count];
                JointCursors = new int[script.JointMaps.Count];
                Script = script;
                ScriptDuration = GetScriptDuration(script);
            }

            float GetScriptDuration(Script sc)
            {
                float duration = 0;
                foreach (var jm in sc.JointMaps)
                {
                    duration = Math.Max(duration, jm.Channel.Duration);
                }
                return duration;
            }
        }

		AnmFile anm;
        private RigidModel rm;
		List<ActiveAnimation> animations = new List<ActiveAnimation>();
        List<ActiveAnimation> completeAnimations = new List<ActiveAnimation>();

        private Func<double> getTotalTime;

        private double accumTime;

        public IEnumerable<NetCmpAnimation> Serialize() =>
            animations.Select(x => new NetCmpAnimation()
            {
                Name = x.Name,
                WorldStartTime = (float) x.WorldStartTime,
                Reverse = x.Reverse,
                Loop = x.Loop,
                Finished = false,
            }).Concat(completeAnimations.Select(x => new NetCmpAnimation()
            {
                Name = x.Name,
                Reverse = x.Reverse,
                Finished = true,
            }));

        public void UpdateAnimations(NetCmpAnimation[] data)
        {
            animations = new List<ActiveAnimation>();
            completeAnimations = new List<ActiveAnimation>();
            foreach (var a in data.Where(x => anm.Scripts.ContainsKey(x.Name)))
            {
                var sc = anm.Scripts[a.Name];
                if (a.Finished)
                {
                    foreach (var jm in sc.JointMaps) {
                        var mdl = Parent == null ? rm : Parent.Model.RigidModel;
                        var joint = mdl.Parts[jm.ChildName].Construct;
                        ChannelFloat angles = 0;
                        float t = a.Reverse ? 0 : jm.Channel.Duration;
                        int jointCursor = 0;
                        if (jm.Channel.HasAngle) angles = jm.Channel.FloatAtTime((float)t, ref jointCursor);
                        var quat = Quaternion.Identity;
                        if (jm.Channel.HasOrientation) quat = jm.Channel.QuaternionAtTime((float) t, ref jointCursor);
                        joint.Update(angles, quat);
                    }
                    completeAnimations.Add(new ActiveAnimation(sc)
                    {
                        Name = a.Name,
                        Reverse = a.Reverse,
                    });
                }
                else
                {
                    FLLog.Debug("Client", $"Add animation starting {a.WorldStartTime}: current {getTotalTime()}");
                    animations.Add(new ActiveAnimation(sc)
                    {
                        Name = a.Name,
                        WorldStartTime = a.WorldStartTime,
                        Loop = a.Loop,
                        Reverse = a.Reverse,
                    });
                }
            }
        }


        public AnimationComponent(GameObject parent, AnmFile animation) : base(parent)
		{
			anm = animation;
            getTotalTime = () => accumTime;
        }

        public AnimationComponent(RigidModel rm, AnmFile animation) : base(null)
        {
            this.rm = rm;
            anm = animation;
            getTotalTime = () => accumTime;
        }



        public void StartAnimation(Script script, bool loop = true, float start_time = 0, float time_scale = 1,
            float duration = 0, bool reverse = false)
        {
            if (Parent?.RenderComponent is CharacterRenderer characterRenderer)
            {
                characterRenderer.Skeleton.StartScript(script, start_time, time_scale, duration, loop);
                return;
            }
            if (active.TryGetValue(script, out var animation))
            {
                if (reverse != animation.Reverse)
                {
                    var currTime = getTotalTime();
                    var t = animation.StartT + (currTime - animation.WorldStartTime) * animation.TimeScale;
                    t = MathHelper.Clamp(t, 0, animation.ScriptDuration);
                    animation.StartT = (float)(animation.ScriptDuration - t);
                    animation.Reverse = reverse;
                    animation.WorldStartTime = getTotalTime();
                    animation.Duration = duration;
                    animation.Loop = loop;
                }
            }
            else
            {
                animations.Add(new ActiveAnimation(script)
                {
                    Name = script.Name,
                    WorldStartTime = getTotalTime(),
                    Loop = loop,
                    Duration = duration,
                    StartT = start_time,
                    TimeScale = time_scale,
                    Reverse = reverse,
                });
                active[script] = animations[^1];
            }
        }
		public void StartAnimation(string animationName, bool loop = true, float start_time = 0, float time_scale = 1, float duration = 0, bool reverse = false)
		{
            if (anm.Scripts.TryGetValue(animationName, out var script))
            {
                StartAnimation(script, loop, start_time, time_scale, duration, reverse);
            }
            else
            {
                FLLog.Error("Animation", animationName + " not present");
            }
		}

        public void ResetAnimations()
        {
            animations.Clear();
            if (Parent != null)
            {
                foreach (var p in Parent.Model.RigidModel.AllParts) p.Construct?.Reset();
                Parent.Model.RigidModel.UpdateTransform();
            } else if (rm != null)
            {
                foreach (var p in rm.AllParts) p.Construct?.Reset();
                rm.UpdateTransform();
            }
        }

        public void ResetTimeSource()
        {
            getTotalTime = () => accumTime;
        }

        public void SetTimeSource(Func<double> time)
        {
            getTotalTime = time;
        }



        public bool HasAnimation(string animationName)
		{
			if (animationName == null) return false;
			return anm.Scripts.ContainsKey(animationName);
		}

		public event Action<string> AnimationCompleted;

		public override void Update(double time)
		{
            if (Parent != null && Parent.RenderComponent is CharacterRenderer characterRenderer)
            {
                characterRenderer.Skeleton.UpdateScripts(time);
                return;
            }
			accumTime += time;
			int c = animations.Count;
			for (int i = animations.Count - 1; i >= 0; i--)
			{
				if (ProcessAnimation(animations[i]))
				{
					if (AnimationCompleted != null)
						AnimationCompleted(animations[i].Name);
                    active.Remove(animations[i].Script);
                    completeAnimations.Add(animations[i]);
					animations.RemoveAt(i);
				}
			}
            if (c > 0 && Parent != null)
            {
                Parent.Model.RigidModel.UpdateTransform();
                Parent.UpdateCollision();
            }
            if (c > 0) rm?.UpdateTransform();
        }

		bool ProcessAnimation(ActiveAnimation a)
		{
			bool finished = true;
            float ts = a.TimeScale <= 0 ? 1 : a.TimeScale;
			for (int i = 0; i < a.Script.ObjectMaps.Count; i++)
			{
				if (!ProcessObjectMap(ref a.Script.ObjectMaps[i], ref a.ObjectCursors[i], a.WorldStartTime, ts, a.Loop))
					finished = false;
			}
            for (int i = 0; i < a.Script.JointMaps.Count; i++)
			{
				if (!ProcessJointMap(ref a.Script.JointMaps[i], ref a.JointCursors[i], a.WorldStartTime, ts, a.Loop, a.Reverse))
					finished = false;
			}
			return finished || (a.Duration > 0 && (getTotalTime() - a.WorldStartTime) >= a.Duration);
		}


		bool ProcessObjectMap(ref ObjectMap om, ref int cur, double startTime, float timeScale, bool loop)
		{
			return false;
		}

		bool ProcessJointMap(ref JointMap jm, ref int cur, double startTime, float timeScale, bool loop, bool reverse)
        {
            var mdl = Parent == null ? rm : Parent.Model.RigidModel;
            var joint = mdl.Parts[jm.ChildName].Construct;
            double t = (getTotalTime() - startTime) * timeScale;
            bool done = false;
			//looping?
            if (!loop && t >= jm.Channel.Duration)
            {
                done = true;
            }
            else
            {
                t = t % jm.Channel.Duration;
            }
            if (reverse)
                t = jm.Channel.Duration - t;
            ChannelFloat angle = 0;
            if (jm.Channel.HasAngle) angle = jm.Channel.FloatAtTime((float)t, ref cur);
            var quat = Quaternion.Identity;
            if (jm.Channel.HasOrientation) quat = jm.Channel.QuaternionAtTime((float)t, ref cur);
            joint.Update(angle, quat);
			return done;
		}

        public float GetPlayPosition(Script sc)
        {
            if (!active.TryGetValue(sc, out var a))
                return 0;
            float ts = a.TimeScale <= 0 ? 1 : a.TimeScale;
            double t = a.StartT + (getTotalTime() - a.WorldStartTime) * ts;
            float duration = 0;
            for (int i = 0; i < sc.JointMaps.Count; i++)
            {
                if (sc.JointMaps[i].Channel.Duration > duration)
                    duration = sc.JointMaps[i].Channel.Duration;
            }
            var v = duration > 0 ? (float)MathHelper.Clamp(t / duration, 0, 1) : 0;
            return a.Reverse ? (1 - v) : v;
        }
	}
}
