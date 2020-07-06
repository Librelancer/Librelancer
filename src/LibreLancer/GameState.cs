// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public abstract class GameState
	{
		protected FreelancerGame Game;
		public GameState (FreelancerGame game)
		{
			Game = game;
		}
		public abstract void Update(TimeSpan delta);
		public abstract void Draw(TimeSpan delta);
        public virtual void OnResize()
        {
        }
		public virtual void Unregister()
		{
		}
        public virtual void Exiting()
        {
        }

        protected void FadeIn(double delay, double time)
        {
            totalTime = 0;
            fadeDelay = delay;
            fadeFirstFrame = true;
            fadeTime = fadeDuration = time;
            fading = true;
        }
        protected void FadeOut(double time, Action toDo)
        {
            fadeTime = fadeDuration = time;
            fadeFirstFrame = true;
            fadeIn = false;
            fading = true;
            fadeDone = toDo;
        }

        bool fading = false;
        private bool fadeFirstFrame = true;
        bool fadeIn = true;
        double fadeDelay;
        double fadeTime;
        double fadeDuration;
        double totalTime = 0;
        private int hitchCount = 0;
        Action fadeDone;
        protected void DoFade(TimeSpan delta)
        {
            if (fading)
            {
                if (delta.TotalSeconds < 0.1 || hitchCount >= 6)
                {
                    if (!fadeFirstFrame)
                        totalTime += delta.TotalSeconds; //Avoid frame hitching
                    else
                        delta = TimeSpan.Zero;
                    fadeFirstFrame = false;
                } else {
                    //don't freeze entirely
                    hitchCount++;
                    delta = TimeSpan.Zero;
                }
                var alpha = (float)(fadeTime / fadeDuration);
                if (alpha < 0) alpha = 0;
                if (!fadeIn) alpha = (1 - alpha);
                Game.Renderer2D.FillRectangle(new Rectangle(0, 0, Game.Width, Game.Height), new Color4(0, 0, 0, alpha));
                if (totalTime > fadeDelay) fadeTime -= delta.TotalSeconds; //Delay fade in
                if (fadeTime < -0.25f) //negative allows last frame
                {
                    fadeDone?.Invoke();
                    fading = false;
                }
            }
        }
    }
}

