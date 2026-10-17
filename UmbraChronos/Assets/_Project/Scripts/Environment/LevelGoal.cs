using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UmbraChronos.Core;
using UmbraChronos.Player;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Attached to the Sanctuary Arch trigger to handle level completion.
    /// When the player steps through the arch:
    /// 1. Disables player input and simulates gentle deceleration.
    /// 2. Fires TimeEvents.OnLevelCompleted and TimeEvents.OnPuzzleSolved.
    /// 3. Plays sanctuary triumph audio.
    /// 4. Fades the screen and smoothly loads the next progression level or returns to MainMenu.
    /// </summary>
    public class LevelGoal : MonoBehaviour
    {
        [Header("Progression")]
        [Tooltip("Name of the scene to load. If empty, automatically progresses (Tutorial -> 01 -> 02 -> 03 -> MainMenu).")]
        [SerializeField] private string nextSceneName = "";

        [Tooltip("Human-readable chamber name displayed on completion banner.")]
        [SerializeField] private string chamberName = "Sanctuary Reached";

        [Tooltip("Delay in seconds before loading the next scene.")]
        [SerializeField] private float transitionDelay = 2.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip triumphSFX;

        private bool isCompleted = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCompleted) return;

            if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
            {
                TriggerCompletion(collision.gameObject);
            }
        }

        public void TriggerCompletion(GameObject playerObj)
        {
            if (isCompleted) return;
            isCompleted = true;

            // 1. Freeze player movement safely
            if (playerObj != null)
            {
                var pc = playerObj.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = false;

                var rb = playerObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y);
                }
            }

            // 2. Play audio
            if (triumphSFX != null)
            {
                AudioSource.PlayClipAtPoint(triumphSFX, transform.position, 1.0f);
            }

            // 3. Fire events
            TimeEvents.OnLevelCompleted?.Invoke(chamberName);
            TimeEvents.OnPuzzleSolved?.Invoke();

            // 4. Begin transition
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            yield return new WaitForSeconds(transitionDelay);

            string targetScene = nextSceneName;
            if (string.IsNullOrEmpty(targetScene))
            {
                string current = SceneManager.GetActiveScene().name;
                if (current.Equals("Level_Tutorial", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetScene = "Level_01";
                }
                else if (current.Equals("Level_01", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetScene = "Level_02";
                }
                else if (current.Equals("Level_02", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetScene = "Level_03";
                }
                else
                {
                    targetScene = "MainMenu";
                }
            }

            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                Debug.LogWarning($"[LevelGoal] Target scene '{targetScene}' cannot be loaded. Loading MainMenu.");
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
