namespace LibreLancer.Interface
{
    /// <summary>
    /// Helper class to manage pending infocard state for faction display.
    /// Used by both SpaceGameplay and RoomGameplay to avoid code duplication.
    /// </summary>
    public class PendingInfocardHelper
    {
        private int _pendingInfocardId;
        private int _pendingInfocardName;

        /// <summary>
        /// Sets the pending infocard IDs to be displayed when InfoWindow opens.
        /// </summary>
        public void Set(int idsInfo, int idsName)
        {
            _pendingInfocardId = idsInfo;
            _pendingInfocardName = idsName;
        }

        /// <summary>
        /// Gets and clears the pending infocard ID.
        /// Returns 0 if no pending infocard.
        /// </summary>
        public int GetAndClearInfocardId()
        {
            int id = _pendingInfocardId;
            _pendingInfocardId = 0;
            return id;
        }

        /// <summary>
        /// Gets and clears the pending infocard name ID.
        /// Returns 0 if no pending name.
        /// </summary>
        public int GetAndClearNameId()
        {
            int id = _pendingInfocardName;
            _pendingInfocardName = 0;
            return id;
        }

        /// <summary>
        /// Checks if there is a pending infocard without clearing it.
        /// </summary>
        public bool HasPending => _pendingInfocardId > 0;
    }
}
