using System;

namespace UmbraChronos.Core
{
    /// <summary>
    /// Global decoupled event bus for temporal, celestial, and shadow gameplay signals.
    /// Eliminates direct tight-coupling between player, time manager, sundial, and puzzles.
    /// </summary>
    public static class TimeEvents
    {
        /// <summary>
        /// Invoked when the celestial sundial / light source angle changes.
        /// Parameter: Current light angle in degrees (0 - 360).
        /// </summary>
        public static Action<float> OnSunAngleChanged;

        /// <summary>
        /// Invoked when rewinding begins (true) or ends (false).
        /// Parameter: true = active rewind, false = resume normal time flow.
        /// </summary>
        public static Action<bool> OnRewindStateChanged;

        /// <summary>
        /// Invoked when an Umbral Echo (silhouette shadow clone) is anchored in the world.
        /// </summary>
        public static Action OnEchoCreated;

        /// <summary>
        /// Invoked when an Umbral Echo expires or dissolves.
        /// </summary>
        public static Action OnEchoDissolved;

        /// <summary>
        /// Invoked when the player dies (e.g. touched lethal sunlight radiation).
        /// </summary>
        public static Action OnPlayerDied;

        /// <summary>
        /// Invoked when a level puzzle room is solved.
        /// </summary>
        public static Action OnPuzzleSolved;
    }
}
