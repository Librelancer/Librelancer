namespace LibreLancer.ImUI;
    /// <summary>
    /// Controls which screen is currently active.
    /// Ensures deterministic screen transitions.
    /// </summary>
    public sealed class ScreenManager
    {
        private Screen current;

        public Screen Current => current;

        /// <summary>
        /// Switches to a new screen.
        /// Calls OnExit() on the old screen and OnEnter() on the new one.
        /// </summary>
        public void SetScreen(Screen next)
        {
            if (current == next)
                return;

            current?.OnExit();
            current = next;
            current?.OnEnter();
        }

        /// <summary>
        /// Draws the active screen.
        /// </summary>
        public void Draw(double elapsed)
        {
            current?.Draw(elapsed);
        }

        /// <summary>
        /// Clears the current screen without replacing it.
        /// </summary>
        public void Clear()
        {
            current?.OnExit();
            current = null;
        }
    }
