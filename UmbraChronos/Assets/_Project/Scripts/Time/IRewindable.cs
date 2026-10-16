namespace UmbraChronos.Time
{
    /// <summary>
    /// Contract implemented by any entity that records and rewinds state (Player, Platforms, Doors, Obstacles).
    /// </summary>
    public interface IRewindable
    {
        /// <summary>
        /// Called on physics ticks during normal gameplay to snapshot current state into history buffer.
        /// </summary>
        void RecordSnapshot();

        /// <summary>
        /// Called on physics ticks while rewind is active to step backward along history buffer.
        /// </summary>
        void RewindSnapshot();

        /// <summary>
        /// Invoked when rewind begins.
        /// </summary>
        void OnStartRewind();

        /// <summary>
        /// Invoked when rewind finishes.
        /// </summary>
        void OnStopRewind();
    }
}
