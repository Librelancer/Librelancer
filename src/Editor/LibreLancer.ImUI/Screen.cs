using ImGuiNET;

namespace LibreLancer.ImUI
{
    /// <summary>
    /// Base class for all ImGui screens.
    /// Screens represent mutually-exclusive UI states.
    /// </summary>
    public abstract class Screen
    {
        protected readonly ScreenManager Screens;
        protected readonly PopupManager Popups;

        protected Screen(ScreenManager screens, PopupManager popups)
        {
            Screens = screens;
            Popups = popups;
        }

        /// <summary>
        /// Called once when the screen becomes active.
        /// Use for initialization and setup.
        /// </summary>
        public virtual void OnEnter()
        {
        }

        /// <summary>
        /// Called once when the screen is removed.
        /// Use for final cleanup only.
        /// </summary>
        public virtual void OnExit()
        {
        }

        /// <summary>
        /// Called every frame to render the screen.
        /// All user decisions must happen here.
        /// </summary>
        public abstract void Draw(double elapsed);

    }
}
