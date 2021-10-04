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
		public abstract void Update(double delta);
		public abstract void Draw(double delta);
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
            hitchCount = 0;
            totalTime = 0;
            fadeDelay = delay;
            fadeFirstFrame = true;
            fadeTime = fadeDuration = time;
            fading = true;
        }
        protected void FadeOut(double time, Action toDo)
        {
            hitchCount = 0;
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
        protected void DoFade(double delta)
        {
            if (fading)
            {
                if (delta < 0.1 || hitchCount >= 6)
                {
                    if (!fadeFirstFrame)
                        totalTime += delta; //Avoid frame hitching
                    else
                        delta = 0;
                    fadeFirstFrame = false;
                } else {
                    //don't freeze entirely
                    hitchCount++;
                    delta = 0;
                }
                var alpha = (float)(fadeTime / fadeDuration);
                if (alpha < 0) alpha = 0;
                if (!fadeIn) alpha = (1 - alpha);
                Game.RenderContext.Renderer2D.FillRectangle(new Rectangle(0, 0, Game.Width, Game.Height), new Color4(0, 0, 0, alpha));
                if (totalTime > fadeDelay) fadeTime -= delta; //Delay fade in
                if (fadeTime < -0.25f) //negative allows last frame
                {
                    fadeDone?.Invoke();
                    fading = false;
                }
            }
        }
    }
}

