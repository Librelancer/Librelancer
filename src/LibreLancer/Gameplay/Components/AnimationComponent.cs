// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf;
namespace LibreLancer
{
	public class AnimationComponent : GameComponent
	{
		class ActiveAnimation
		{
			public string Name;
			public Script Script;
			public double StartTime;
			public bool Loop;
		}

		AnmFile anm;
        ConstructCollection constructs;
		List<ActiveAnimation> animations = new List<ActiveAnimation>();
		public AnimationComponent(GameObject parent, AnmFile animation) : base(parent)
		{
			anm = animation;
		}

        public AnimationComponent(ConstructCollection constructs, AnmFile animation) : base(null)
        {
            anm = animation;
            this.constructs = constructs;
        }
		public void StartAnimation(string animationName, bool loop = true)
		{
			if (anm.Scripts.ContainsKey(animationName))
			{
				var sc = anm.Scripts[animationName];
				animations.Add(new ActiveAnimation() { Name = animationName, Script = sc, StartTime = totalTime, Loop = loop });
			}
			else
				FLLog.Error("Animation", animationName + " not present");
		}

        public void ResetAnimations()
        {
            animations.Clear();
            foreach(var c in constructs) c.Reset();
        }

        public bool HasAnimation(string animationName)
		{
			if (animationName == null) return false;
			return anm.Scripts.ContainsKey(animationName);
		}

		public event Action<string> AnimationCompleted;

		double totalTime = 0;
		public override void Update(TimeSpan time)
		{
            if (constructs == null) constructs = Parent.CmpConstructs;
			totalTime += time.TotalSeconds;
			int c = animations.Count;
			for (int i = animations.Count - 1; i >= 0; i--)
			{
				if (ProcessAnimation(animations[i]))
				{
					if (AnimationCompleted != null)
						AnimationCompleted(animations[i].Name);
					animations.RemoveAt(i);
				}
			}
			if (c > 0 && Parent != null)
				Parent.UpdateCollision();
		}

		bool ProcessAnimation(ActiveAnimation a)
		{
			bool finished = true;
			foreach (var map in a.Script.ObjectMaps)
			{
				if (!ProcessObjectMap(map, a.StartTime, a.Loop))
					finished = false;
			}
			foreach (var map in a.Script.JointMaps)
			{
				if (!ProcessJointMap(map, a.StartTime, a.Loop))
					finished = false;
			}
			return finished;
		}

		bool ProcessObjectMap(ObjectMap om, double startTime, bool loop)
		{
			return false;
		}

		bool ProcessJointMap(JointMap jm, double startTime, bool loop)
		{
			var joint = constructs.Find(jm.ChildName);
			double t = totalTime - startTime;
			//looping?
			if (jm.Channel.Interval == -1)
			{
				var duration = jm.Channel.Frames[jm.Channel.FrameCount - 1].Time.Value;
				if (!loop && t >= duration)
					return true;
				else
					t = t % duration;
			}
			float t1 = 0;
			for (int i = 0; i < jm.Channel.Frames.Length - 1; i++)
			{
				var t0 = jm.Channel.Frames[i].Time ?? (jm.Channel.Interval * i);
				t1 = jm.Channel.Frames[i + 1].Time ?? (jm.Channel.Interval * (i + 1));
				var v1 = jm.Channel.Frames[i].JointValue;
				var v2 = jm.Channel.Frames[i + 1].JointValue;
				if (t >= t0 && t <= t1)
				{
					var dist = Math.Abs(v2 - v1);
					if (Math.Abs(t1 - t0) < 0.5f && dist > 1f)
					{
						//Deal with the horrible rotation scripts
						//Disable interpolation between e.g.  3.137246 to -3.13287
						//Don't remove this or things go crazy ~ Callum
						joint.Update((float)v2);
					}
					else
					{
						var x = (t - t0) / (t1 - t0);
						var val = v1 + (v2 - v1) * x;
						joint.Update((float)val);
					}
				}
			}
			return false;
		}
	}
}
