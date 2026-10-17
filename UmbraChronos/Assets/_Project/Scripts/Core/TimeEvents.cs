using System;
using UnityEngine;

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
        /// Invoked when the rewind buffer energy changes.
        /// Parameters: float currentEnergy, float maxEnergy.
        /// </summary>
        public static Action<float, float> OnRewindEnergyChanged;

        /// <summary>
        /// Invoked when an Umbral Echo (silhouette shadow clone) is anchored in the world.
        /// </summary>
        public static Action OnEchoCreated;

        /// <summary>
        /// Invoked when an Umbral Echo expires or dissolves.
        /// </summary>
        public static Action OnEchoDissolved;

        /// <summary>
        /// Invoked when the player's direct sunlight exposure changes.
        /// Parameter: Exposure fraction from 0f (fully safe in shadow) to 1f (fatal burn).
        /// </summary>
        public static Action<float> OnSunlightExposureChanged;

        /// <summary>
        /// Invoked when the player dies (e.g. touched lethal sunlight radiation).
        /// </summary>
        public static Action OnPlayerDied;

        /// <summary>
        /// Invoked when the player respawns or rewinds back to life.
        /// </summary>
        public static Action OnPlayerRespawned;

        /// <summary>
        /// Invoked when a level puzzle room is solved.
        /// </summary>
        public static Action OnPuzzleSolved;

        /// <summary>
        /// Invoked when the player jumps off the ground.
        /// </summary>
        public static Action OnPlayerJumped;

        /// <summary>
        /// Invoked when the player lands back on solid ground.
        /// Parameter: float fallSpeed (impact velocity magnitude).
        /// </summary>
        public static Action<float> OnPlayerLanded;

        /// <summary>
        /// Invoked when a pressure plate changes state (pressed = true, released = false).
        /// </summary>
        public static Action<bool> OnPressurePlateChanged;

        /// <summary>
        /// Invoked when a timed gate changes state (opened = true, closed = false).
        /// </summary>
        public static Action<bool> OnGateStateChanged;

        /// <summary>
        /// Invoked when the camera shifts to a new room/screen.
        /// Parameter: Vector2Int new room index.
        /// </summary>
        public static Action<Vector2Int> OnRoomChanged;

        /// <summary>
        /// Invoked to trigger screen shake / camera trauma.
        /// Parameters: float intensity, float duration.
        /// </summary>
        public static Action<float, float> OnCameraShake;

        /// <summary>
        /// Invoked when the player's remaining rewind chances change.
        /// Parameters: int remainingChances, int maxChances.
        /// </summary>
        public static Action<int, int> OnLivesChanged;

        /// <summary>
        /// Invoked when all rewind chances are exhausted (perma-death Game Over).
        /// </summary>
        public static Action OnGameOver;

        /// <summary>
        /// Invoked when a level is completed upon reaching the Sanctuary Arch.
        /// Parameter: string chamberName.
        /// </summary>
        public static Action<string> OnLevelCompleted;
    }
}
