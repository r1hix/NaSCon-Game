using System.Collections.Generic;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Time
{
    /// <summary>
    /// An instantiated shadow clone (Umbral Echo) that replays a recorded trajectory in forward time.
    /// Possesses a physical collider to trigger pressure plates and serve as a physical platform.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UmbralEcho : MonoBehaviour
    {
        [Header("Echo Settings")]
        [Tooltip("Seconds the echo remains solid after finishing its playback before dissolving")]
        [SerializeField] private float lingerDuration = 2.5f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color echoColor = new Color(0.12f, 0.11f, 0.29f, 0.75f); // #1E1B4B at 75% alpha

        private List<TimeBody.TimeSnapshot> playbackFrames;
        private int currentFrameIndex;
        private bool isPlaying;
        private float lingerTimer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = echoColor;
            }
        }

        /// <summary>
        /// Initializes the echo with the recorded trajectory and begins forward playback.
        /// </summary>
        public void Initialize(List<TimeBody.TimeSnapshot> trajectory)
        {
            if (trajectory == null || trajectory.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            playbackFrames = trajectory;
            currentFrameIndex = 0;
            isPlaying = true;
            lingerTimer = lingerDuration;

            // Snap to initial position
            transform.position = playbackFrames[0].position;
            transform.rotation = Quaternion.Euler(0f, 0f, playbackFrames[0].rotation);

            TimeEvents.OnEchoCreated?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!isPlaying) return;

            if (currentFrameIndex < playbackFrames.Count)
            {
                TimeBody.TimeSnapshot frame = playbackFrames[currentFrameIndex];
                transform.position = frame.position;
                transform.rotation = Quaternion.Euler(0f, 0f, frame.rotation);
                currentFrameIndex++;
            }
            else
            {
                // Finished forward playback; linger in final pose
                lingerTimer -= UnityEngine.Time.fixedDeltaTime;
                if (lingerTimer <= 0f)
                {
                    Dissolve();
                }
            }
        }

        /// <summary>
        /// Fades out and destroys the echo.
        /// </summary>
        public void Dissolve()
        {
            isPlaying = false;
            TimeEvents.OnEchoDissolved?.Invoke();
            Destroy(gameObject);
        }
    }
}
