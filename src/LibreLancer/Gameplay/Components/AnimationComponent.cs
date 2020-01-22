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
            if (Parent.RenderComponent is CharacterRenderer characterRenderer)
            {
                if (anm.Scripts.TryGetValue(animationName, out Script sc))
                    characterRenderer.Skeleton.StartScript(sc);
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
            if (Parent.RenderComponent is CharacterRenderer characterRenderer)
            {
                characterRenderer.Skeleton.UpdateScripts(time);
                return;
            }
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
