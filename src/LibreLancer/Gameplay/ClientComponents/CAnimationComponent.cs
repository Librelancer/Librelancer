// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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
        private RigidModel rm;
		List<ActiveAnimation> animations = new List<ActiveAnimation>();
		public AnimationComponent(GameObject parent, AnmFile animation) : base(parent)
		{
			anm = animation;
		}

        public AnimationComponent(RigidModel rm, AnmFile animation) : base(null)
        {
            this.rm = rm;
            anm = animation;
        }
		public void StartAnimation(string animationName, bool loop = true, float start_time = 0, float time_scale = 1, float duration = 0)
		{
            if (Parent?.RenderComponent is CharacterRenderer characterRenderer)
            {
                if (anm.Scripts.TryGetValue(animationName, out Script sc))
                    characterRenderer.Skeleton.StartScript(sc, start_time, time_scale, duration, loop);
                return;
            }
			if (anm.Scripts.ContainsKey(animationName))
			{
				var sc = anm.Scripts[animationName];
				animations.Add(new ActiveAnimation() { Name = animationName, Script = sc, StartTime = totalTime, Loop = loop });
			}
			else
				FLLog.Error("Animation", animationName + " not present");
		}

        public void WarpTime(double totalTime)
        {
            this.totalTime = totalTime;
        }

        public void ResetAnimations()
        {
            animations.Clear();
            if (Parent != null)
            {
                foreach (var p in Parent.RigidModel.AllParts) p.Construct?.Reset();
                Parent.RigidModel.UpdateTransform();
            } else if (rm != null)
            {
                foreach (var p in rm.AllParts) p.Construct?.Reset();
                rm.UpdateTransform();
            }
        }

        public bool HasAnimation(string animationName)
		{
			if (animationName == null) return false;
			return anm.Scripts.ContainsKey(animationName);
		}

		public event Action<string> AnimationCompleted;

		double totalTime = 0;
		public override void Update(double time)
		{
            if (Parent != null && Parent.RenderComponent is CharacterRenderer characterRenderer)
            {
                characterRenderer.Skeleton.UpdateScripts(time);
                return;
            }
			totalTime += time;
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
            {
                Parent.RigidModel.UpdateTransform();
                Parent.UpdateCollision();
            }
            if (c > 0) rm?.UpdateTransform();
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
            var mdl = Parent == null ? rm : Parent.RigidModel;
            var joint = mdl.Parts[jm.ChildName].Construct;
			double t = totalTime - startTime;
			//looping?
			if (jm.Channel.Interval == -1)
			{
				if (!loop && t >= jm.Channel.Duration)
					return true;
				else
					t = t % jm.Channel.Duration;
			}

            float angle = 0;
            if (jm.Channel.HasAngle) angle = jm.Channel.AngleAtTime((float) t);
            var quat = Quaternion.Identity;
            if (jm.Channel.HasOrientation) quat = jm.Channel.QuaternionAtTime((float) t);
            joint.Update(angle, quat);
			return false;
		}
	}
}
