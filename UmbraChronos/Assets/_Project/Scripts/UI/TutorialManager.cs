using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UmbraChronos.Core;

namespace UmbraChronos.UI
{
    /// <summary>
    /// In-world animated tutorial prompt system for Umbra Chronos.
    /// Places world-space floating text cards at key locations in the tutorial level.
    /// Each prompt triggers when the player enters its activation radius and dismisses
    /// after a duration or when the player leaves + a delay.
    /// Features:
    /// - Smooth fade-in / fade-out with configurable duration
    /// - Gentle vertical bob animation (sine wave)
    /// - Cyan runic glow text (#38BDF8) on dark obsidian backdrop
    /// - Event-driven prompts that react to player actions (sundial rotation, rewind, echo)
    /// - Sequential gating: certain prompts only appear after prior ones are dismissed
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Tutorial Style")]
        [SerializeField] private float fadeSpeed = 3.0f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobFrequency = 1.5f;
        [SerializeField] private Color textColor = new Color(0.22f, 0.74f, 0.97f, 1f);       // #38BDF8 Cyan Rune
        [SerializeField] private Color headerColor = new Color(1f, 0.82f, 0.40f, 1f);         // #FFD166 Warm Gold
        [SerializeField] private Color backdropColor = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Semi-transparent obsidian
        [SerializeField] private Color keyGlyphColor = new Color(0.66f, 0.33f, 0.97f, 1f);    // #A855F7 Violet glyph

        // Internal list of all tutorial prompts in the level
        private List<TutorialPrompt> prompts = new List<TutorialPrompt>();
        private int highestCompletedIndex = -1;

        // Tracking player actions for event-driven prompts
        private bool hasSunDialRotated = false;
        private bool hasRewound = false;
        private bool hasSpawnedEcho = false;
        private bool hasSteppedOnPlate = false;
        private float sunAngle = 90f;

        public int HighestCompletedIndex => highestCompletedIndex;

        /// <summary>
        /// Represents a single floating tutorial card in the world.
        /// </summary>
        public class TutorialPrompt
        {
            public int index;
            public string headerText;
            public string bodyText;
            public string keyHintText;          // e.g. "[Q] / [E]"
            public Vector3 worldPosition;       // anchor position in world
            public float activationRadius;      // player must be within this radius
            public float displayDuration;       // 0 = infinite (dismiss by leaving)
            public int requiredPriorIndex;      // -1 = no prerequisite
            public bool requiresSunDialAction;
            public bool requiresRewindAction;
            public bool requiresEchoAction;
            public bool requiresPlateAction;

            // Runtime state
            public GameObject canvasRoot;
            public CanvasGroup canvasGroup;
            public RectTransform cardRect;
            public TextMeshProUGUI headerTMP;
            public TextMeshProUGUI bodyTMP;
            public TextMeshProUGUI keyHintTMP;
            public Image backdropImage;
            public float currentAlpha;
            public float displayTimer;
            public bool hasBeenShown;
            public bool isCompleted;
            public float baseY;
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnEchoCreated += HandleEchoCreated;
            TimeEvents.OnPressurePlateChanged += HandlePlateChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnEchoCreated -= HandleEchoCreated;
            TimeEvents.OnPressurePlateChanged -= HandlePlateChanged;
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var pc = Object.FindAnyObjectByType<Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var pc = Object.FindAnyObjectByType<Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
                return;
            }

            for (int i = 0; i < prompts.Count; i++)
            {
                UpdatePrompt(prompts[i]);
            }
        }

        private void UpdatePrompt(TutorialPrompt p)
        {
            if (p.isCompleted || p.canvasRoot == null) return;

            // Check prerequisites
            bool prereqMet = p.requiredPriorIndex < 0 || highestCompletedIndex >= p.requiredPriorIndex;
            if (!prereqMet)
            {
                // Fade out if visible
                FadePrompt(p, false);
                return;
            }

            // Check action requirements (event gating)
            if (p.requiresSunDialAction && !hasSunDialRotated) { FadePrompt(p, false); return; }
            if (p.requiresRewindAction && !hasRewound) { FadePrompt(p, false); return; }
            if (p.requiresEchoAction && !hasSpawnedEcho) { FadePrompt(p, false); return; }
            if (p.requiresPlateAction && !hasSteppedOnPlate) { FadePrompt(p, false); return; }

            // Check proximity
            float dist = Vector2.Distance(playerTransform.position, p.worldPosition);
            bool inRange = dist <= p.activationRadius;

            if (inRange && !p.hasBeenShown)
            {
                p.hasBeenShown = true;
                p.displayTimer = p.displayDuration;
            }

            bool shouldShow = false;
            if (p.hasBeenShown)
            {
                if (p.displayDuration > 0f)
                {
                    p.displayTimer -= UnityEngine.Time.deltaTime;
                    shouldShow = p.displayTimer > 0f;
                    if (p.displayTimer <= 0f)
                    {
                        CompletePrompt(p);
                    }
                }
                else
                {
                    // Infinite duration: stay visible while in range
                    shouldShow = inRange;
                    if (!inRange && p.hasBeenShown)
                    {
                        // Player left the area — mark as completed
                        CompletePrompt(p);
                    }
                }
            }

            FadePrompt(p, shouldShow);

            // Bob animation
            if (p.currentAlpha > 0.01f && p.cardRect != null)
            {
                float bob = Mathf.Sin(UnityEngine.Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
                p.canvasRoot.transform.position = new Vector3(p.worldPosition.x, p.baseY + bob, p.worldPosition.z);
            }
        }

        private void FadePrompt(TutorialPrompt p, bool visible)
        {
            float target = visible ? 1f : 0f;
            p.currentAlpha = Mathf.MoveTowards(p.currentAlpha, target, fadeSpeed * UnityEngine.Time.deltaTime);
            if (p.canvasGroup != null)
            {
                p.canvasGroup.alpha = p.currentAlpha;
            }
        }

        private void CompletePrompt(TutorialPrompt p)
        {
            if (p.isCompleted) return;
            p.isCompleted = true;
            if (p.index > highestCompletedIndex)
            {
                highestCompletedIndex = p.index;
            }
        }

        #region Event Handlers

        private void HandleSunAngleChanged(float angle)
        {
            // Consider rotated if angle changed by >10 degrees from starting 90
            if (Mathf.Abs(angle - 90f) > 10f)
            {
                hasSunDialRotated = true;
            }
            sunAngle = angle;
        }

        private void HandleRewindStateChanged(bool active)
        {
            if (active) hasRewound = true;
        }

        private void HandleEchoCreated()
        {
            hasSpawnedEcho = true;
        }

        private void HandlePlateChanged(bool pressed)
        {
            if (pressed) hasSteppedOnPlate = true;
        }

        #endregion

        #region Public API — Prompt Registration

        /// <summary>
        /// Registers a tutorial prompt and builds its world-space UI canvas.
        /// Call this from TutorialBuilder or from a runtime setup script.
        /// </summary>
        public TutorialPrompt RegisterPrompt(
            int index,
            string header,
            string body,
            string keyHint,
            Vector3 worldPos,
            float radius = 4f,
            float duration = 0f,
            int requiredPrior = -1,
            bool requiresSunDial = false,
            bool requiresRewind = false,
            bool requiresEcho = false,
            bool requiresPlate = false)
        {
            var p = new TutorialPrompt
            {
                index = index,
                headerText = header,
                bodyText = body,
                keyHintText = keyHint,
                worldPosition = worldPos,
                activationRadius = radius,
                displayDuration = duration,
                requiredPriorIndex = requiredPrior,
                requiresSunDialAction = requiresSunDial,
                requiresRewindAction = requiresRewind,
                requiresEchoAction = requiresEcho,
                requiresPlateAction = requiresPlate,
                currentAlpha = 0f,
                displayTimer = 0f,
                hasBeenShown = false,
                isCompleted = false,
                baseY = worldPos.y
            };

            BuildPromptCanvas(p);
            prompts.Add(p);
            return p;
        }

        private void BuildPromptCanvas(TutorialPrompt p)
        {
            // World-space Canvas for in-world floating text
            GameObject canvasObj = new GameObject($"Tutorial_Prompt_{p.index}");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.position = p.worldPosition;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Above all gameplay elements

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100f;

            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(6.0f, 2.2f); // world units
            canvasRect.localScale = Vector3.one * 0.02f;      // scale down for world space

            // Card backdrop panel
            GameObject cardObj = new GameObject("Card");
            cardObj.transform.SetParent(canvasObj.transform, false);

            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(300f, 110f);
            cardRect.anchoredPosition = Vector2.zero;

            Image bg = cardObj.AddComponent<Image>();
            bg.color = backdropColor;
            bg.raycastTarget = false;

            // Add rounded corners look via outline
            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = new Color(textColor.r, textColor.g, textColor.b, 0.4f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            // Header text
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);

            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.65f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = new Vector2(10f, 0f);
            headerRect.offsetMax = new Vector2(-10f, -5f);

            TextMeshProUGUI headerTMP = headerObj.AddComponent<TextMeshProUGUI>();
            headerTMP.text = p.headerText;
            headerTMP.fontSize = 22f;
            headerTMP.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            headerTMP.color = headerColor;
            headerTMP.alignment = TextAlignmentOptions.Center;
            headerTMP.textWrappingMode = TextWrappingModes.Normal;
            headerTMP.raycastTarget = false;

            // Body text
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(cardObj.transform, false);

            RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.2f);
            bodyRect.anchorMax = new Vector2(1f, 0.65f);
            bodyRect.offsetMin = new Vector2(10f, 0f);
            bodyRect.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI bodyTMP = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyTMP.text = p.bodyText;
            bodyTMP.fontSize = 14f;
            bodyTMP.color = textColor;
            bodyTMP.alignment = TextAlignmentOptions.Center;
            bodyTMP.textWrappingMode = TextWrappingModes.Normal;
            bodyTMP.raycastTarget = false;

            // Key hint text (bottom)
            GameObject keyObj = new GameObject("KeyHint");
            keyObj.transform.SetParent(cardObj.transform, false);

            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 0f);
            keyRect.anchorMax = new Vector2(1f, 0.2f);
            keyRect.offsetMin = new Vector2(10f, 5f);
            keyRect.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI keyTMP = keyObj.AddComponent<TextMeshProUGUI>();
            keyTMP.text = p.keyHintText;
            keyTMP.fontSize = 16f;
            keyTMP.fontStyle = FontStyles.Bold;
            keyTMP.color = keyGlyphColor;
            keyTMP.alignment = TextAlignmentOptions.Center;
            keyTMP.textWrappingMode = TextWrappingModes.Normal;
            keyTMP.raycastTarget = false;

            // CanvasGroup for alpha fading
            CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Store references
            p.canvasRoot = canvasObj;
            p.canvasGroup = cg;
            p.cardRect = cardRect;
            p.headerTMP = headerTMP;
            p.bodyTMP = bodyTMP;
            p.keyHintTMP = keyTMP;
            p.backdropImage = bg;
        }

        #endregion
    }
}
