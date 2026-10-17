using System.Collections.Generic;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Time
{
    /// <summary>
    /// Attach to any GameObject that needs to record its state and rewind backwards in time.
    /// Stores a circular buffer of physics snapshots (default 300 frames ~ 5 seconds at 60Hz).
    /// </summary>
    public class TimeBody : MonoBehaviour, IRewindable
    {
        [System.Serializable]
        public struct TimeSnapshot
        {
            public Vector2 position;
            public Vector2 velocity;
            public float rotation;

            public TimeSnapshot(Vector2 pos, Vector2 vel, float rot)
            {
                position = pos;
                velocity = vel;
                rotation = rot;
            }
        }

        [Header("Buffer Configuration")]
        [Tooltip("Max recorded frames in the history buffer (300 frames = 5s at 60Hz / 6s at 50Hz)")]
        [SerializeField] private int maxFrames = 300;

        private readonly LinkedList<TimeSnapshot> historyBuffer = new LinkedList<TimeSnapshot>();
        private readonly List<TimeSnapshot> lastRewoundActions = new List<TimeSnapshot>();
        private Rigidbody2D rb;
        private bool isRewinding;
        private bool wasKinematic;

        public int RecordedFramesCount => historyBuffer.Count;
        public float RecordedSeconds => historyBuffer.Count * UnityEngine.Time.fixedDeltaTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
        }

        private void FixedUpdate()
        {
            if (isRewinding)
            {
                RewindSnapshot();
            }
            else
            {
                RecordSnapshot();
            }
        }

        public void RecordSnapshot()
        {
            Vector2 pos = transform.position;
            Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
            float rot = transform.eulerAngles.z;

            // Keep buffer capped to maxFrames
            if (historyBuffer.Count >= maxFrames)
            {
                historyBuffer.RemoveLast();
            }

            historyBuffer.AddFirst(new TimeSnapshot(pos, vel, rot));
        }

        public void RewindSnapshot()
        {
            if (historyBuffer.Count > 0)
            {
                TimeSnapshot snapshot = historyBuffer.First.Value;
                historyBuffer.RemoveFirst();
                lastRewoundActions.Add(snapshot);

                transform.position = snapshot.position;
                transform.rotation = Quaternion.Euler(0f, 0f, snapshot.rotation);

                if (rb != null)
                {
                    rb.linearVelocity = snapshot.velocity;
                }
            }
            else
            {
                // Buffer exhausted: automatically stop rewind for this entity
                OnStopRewind();
            }
        }

        public void OnStartRewind()
        {
            isRewinding = true;
            lastRewoundActions.Clear();

            if (rb != null)
            {
                wasKinematic = rb.bodyType == RigidbodyType2D.Kinematic;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void OnStopRewind()
        {
            isRewinding = false;
            if (rb != null)
            {
                rb.bodyType = wasKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            }
        }

        private void HandleRewindStateChanged(bool active)
        {
            if (active)
            {
                OnStartRewind();
            }
            else
            {
                OnStopRewind();
            }
        }

        /// <summary>
        /// Returns the optimal trajectory for the Umbral Echo.
        /// If the player recently rewound, returns the exact path rewound (playing forward from current rewound pos).
        /// Otherwise returns the player's recent forward history.
        /// </summary>
        public List<TimeSnapshot> GetEchoTrajectory()
        {
            if (lastRewoundActions.Count >= 5)
            {
                var forwardRewound = new List<TimeSnapshot>(lastRewoundActions);
                forwardRewound.Reverse();
                return forwardRewound;
            }

            return GetHistoryClone();
        }

        /// <summary>
        /// Exports a clone of the current recorded buffer (used to initialize an Umbral Echo clone).
        /// </summary>
        public List<TimeSnapshot> GetHistoryClone()
        {
            var list = new List<TimeSnapshot>(historyBuffer);
            list.Reverse(); // Reverse so it plays in forward chronological order!
            return list;
        }

        /// <summary>
        /// Clears all recorded history.
        /// </summary>
        public void ClearHistory()
        {
            historyBuffer.Clear();
        }
    }
}
