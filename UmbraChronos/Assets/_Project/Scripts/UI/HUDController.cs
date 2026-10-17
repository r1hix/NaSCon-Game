using System;
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
    /// Comprehensive HUD & Controls Overlay controller for Umbra Chronos ("Ink & Solar Flare").
    /// Subscribes to TimeEvents to display:
    /// 1. Celestial Sundial Compass (top-right): Rotating sun needle, phase tag (Dawn/Zenith/Dusk/Eclipse), angle readout.
    /// 2. Temporal Rewind Gauge (top-left): Chronos tape reel energy bar, buffer countdown (0-5s), pulsing rewind alert.
    /// 3. Runic Key Prompt Pills (bottom-left): Interactive pills that glow and scale when inputs are pressed.
    /// 4. Solar Hazard Warning (full-screen): Searing red/amber vignette flare and alert banner when in direct lethal sunlight.
    /// 5. Time Fracture Death Overlay: Prompts [HOLD R TO REWIND TIME] upon player vaporization.
    /// 6. Level Title Card: Displays level name and atmospheric motto on spawn/room change.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("1. Sundial Compass (Top-Right)")]
        [SerializeField] private RectTransform compassNeedle;
        [SerializeField] private Image compassSunIcon;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI angleText;
        [SerializeField] private Color colorDawn = new Color(0.96f, 0.25f, 0.37f, 1f);     // #F43F5E Coral Dawn
        [SerializeField] private Color colorZenith = new Color(0.22f, 0.74f, 0.97f, 1f);   // #38BDF8 Azure Zenith
        [SerializeField] private Color colorDusk = new Color(0.96f, 0.62f, 0.04f, 1f);     // #F59E0B Amber Dusk
        [SerializeField] private Color colorEclipse = new Color(0.66f, 0.33f, 0.97f, 1f);  // #A855F7 Cosmic Violet

        [Header("2. Rewind Tape Reel Gauge (Top-Left)")]
        [SerializeField] private Image rewindBarFill;
        [SerializeField] private RectTransform rewindTapeSpool;
        [SerializeField] private TextMeshProUGUI rewindBufferText;
        [SerializeField] private TextMeshProUGUI rewindStatusText;
        [SerializeField] private Color rewindReadyColor = new Color(0.22f, 0.74f, 0.97f, 1f); // #38BDF8
        [SerializeField] private Color rewindActiveColor = new Color(0.66f, 0.33f, 0.97f, 1f); // #A855F7

        [Header("3. Interactive Input Prompt Pills (Bottom)")]
        [SerializeField] private Image pillMoveBg;
        [SerializeField] private Image pillJumpBg;
        [SerializeField] private Image pillSundialBg;
        [SerializeField] private Image pillRewindBg;
        [SerializeField] private Image pillEchoBg;
        [SerializeField] private Color pillNormalColor = new Color(0.08f, 0.08f, 0.12f, 0.75f);
        [SerializeField] private Color pillActiveColor = new Color(0.22f, 0.74f, 0.97f, 0.95f);
        [SerializeField] private Color pillRewindActiveColor = new Color(0.66f, 0.33f, 0.97f, 0.95f);

        [Header("4. Solar Hazard Warning")]
        [SerializeField] private Image solarDangerVignette;
        [SerializeField] private GameObject solarAlertBanner;
        [SerializeField] private TextMeshProUGUI solarAlertText;

        [Header("5. Time Fracture (Death Flow)")]
        [SerializeField] private CanvasGroup deathOverlayGroup;
        [SerializeField] private TextMeshProUGUI deathPromptText;

        [Header("6. Level Title Banner")]
        [SerializeField] private CanvasGroup levelTitleGroup;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI levelSubtitleText;

        // Internal State
        private float currentSunAngle = 90f;
        private float targetNeedleZ = 0f;
        private float currentEnergy = 5f;
        private float maxEnergy = 5f;
        private bool isRewinding = false;
        private bool isDead = false;
        private float exposureFraction = 0f;
        private float titleTimer = 0f;

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnRewindEnergyChanged += HandleRewindEnergyChanged;
            TimeEvents.OnSunlightExposureChanged += HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
            TimeEvents.OnRoomChanged += HandleRoomChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnRewindEnergyChanged -= HandleRewindEnergyChanged;
            TimeEvents.OnSunlightExposureChanged -= HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
            TimeEvents.OnRoomChanged -= HandleRoomChanged;
        }

        private void Start()
        {
            ShowLevelTitle("UMBRA CHRONOS", "Light is Lethal. Shadows are Matter.", 3.5f);
            if (deathOverlayGroup != null) deathOverlayGroup.alpha = 0f;
            if (solarDangerVignette != null)
            {
                Color c = solarDangerVignette.color;
                c.a = 0f;
                solarDangerVignette.color = c;
            }
        }

        private void Update()
        {
            UpdateSundialCompassVisuals();
            UpdateRewindGaugeVisuals();
            UpdateInputPillHighlights();
            UpdateSolarExposureVisuals();
            UpdateDeathOverlayVisuals();
            UpdateLevelTitleBanner();
        }

        #region Event Callbacks

        private void HandleSunAngleChanged(float angle)
        {
            currentSunAngle = angle;
        }

        private void HandleRewindStateChanged(bool rewinding)
        {
            isRewinding = rewinding;
            if (rewinding && isDead)
            {
                isDead = false;
            }
        }

        private void HandleRewindEnergyChanged(float current, float max)
        {
            currentEnergy = current;
            maxEnergy = max > 0.01f ? max : 5f;
        }

        private void HandleSunlightExposureChanged(float fraction)
        {
            exposureFraction = Mathf.Clamp01(fraction);
        }

        private void HandlePlayerDied()
        {
            isDead = true;
        }

        private void HandlePlayerRespawned()
        {
            isDead = false;
        }

        private void HandleRoomChanged(Vector2Int roomIndex)
        {
            ShowLevelTitle($"ROOM [{roomIndex.x + 1}]", "The Sundial Arc Pivots", 2.2f);
        }

        #endregion

        #region Visual Update Loops

        private void UpdateSundialCompassVisuals()
        {
            // Map angle theta: 90 deg = overhead (top of dial, 0 rotation)
            // 20 deg = dawn (left, +70 deg rotation)
            // 160 deg = dusk (right, -70 deg rotation)
            targetNeedleZ = 90f - currentSunAngle;

            if (compassNeedle != null)
            {
                float currentZ = compassNeedle.localEulerAngles.z;
                float newZ = Mathf.MoveTowardsAngle(currentZ, targetNeedleZ, 280f * UnityEngine.Time.deltaTime);
                compassNeedle.localRotation = Quaternion.Euler(0f, 0f, newZ);
            }

            // Determine Phase Name & Color
            string phaseName;
            Color phaseColor;

            if (currentSunAngle <= 55f)
            {
                phaseName = "DAWN (LOW)";
                phaseColor = colorDawn;
            }
            else if (currentSunAngle >= 125f)
            {
                phaseName = "DUSK (LOW)";
                phaseColor = colorDusk;
            }
            else
            {
                phaseName = "ZENITH (NOON)";
                phaseColor = colorZenith;
            }

            if (phaseText != null)
            {
                phaseText.text = phaseName;
                phaseText.color = phaseColor;
            }

            if (angleText != null)
            {
                angleText.text = $"{Mathf.RoundToInt(currentSunAngle)}° ARC";
            }

            if (compassSunIcon != null)
            {
                compassSunIcon.color = phaseColor;
            }
        }

        private void UpdateRewindGaugeVisuals()
        {
            float ratio = maxEnergy > 0f ? Mathf.Clamp01(currentEnergy / maxEnergy) : 1f;

            if (rewindBarFill != null)
            {
                rewindBarFill.fillAmount = Mathf.Lerp(rewindBarFill.fillAmount, ratio, 18f * UnityEngine.Time.deltaTime);
                rewindBarFill.color = isRewinding ? rewindActiveColor : rewindReadyColor;
            }

            if (rewindBufferText != null)
            {
                rewindBufferText.text = $"{currentEnergy:F1}s / {maxEnergy:F1}s";
            }

            if (rewindStatusText != null)
            {
                if (isRewinding)
                {
                    float pulse = (Mathf.Sin(UnityEngine.Time.time * 24f) * 0.5f) + 0.5f;
                    rewindStatusText.text = "REWINDING...";
                    rewindStatusText.color = Color.Lerp(rewindActiveColor, Color.white, pulse);
                }
                else if (ratio >= 0.99f)
                {
                    rewindStatusText.text = "BUFFER FULL";
                    rewindStatusText.color = rewindReadyColor;
                }
                else
                {
                    rewindStatusText.text = "RECHARGING";
                    rewindStatusText.color = Color.gray;
                }
            }

            // Spin tape spool during rewind
            if (rewindTapeSpool != null)
            {
                if (isRewinding)
                {
                    rewindTapeSpool.Rotate(0f, 0f, 450f * UnityEngine.Time.deltaTime);
                }
                else
                {
                    rewindTapeSpool.Rotate(0f, 0f, 15f * UnityEngine.Time.deltaTime);
                }
            }
        }

        private void UpdateInputPillHighlights()
        {
            bool movePressed = false;
            bool jumpPressed = false;
            bool sunPressed = false;
            bool rewindPressed = isRewinding;
            bool echoPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                movePressed = kb.aKey.isPressed || kb.dKey.isPressed || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed;
                jumpPressed = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
                sunPressed = kb.qKey.isPressed || kb.eKey.isPressed || kb.leftBracketKey.isPressed || kb.rightBracketKey.isPressed;
                rewindPressed = rewindPressed || kb.rKey.isPressed || kb.leftShiftKey.isPressed;
                echoPressed = kb.fKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (mouse != null && Mathf.Abs(mouse.scroll.y.ReadValue()) > 0.05f)
            {
                sunPressed = true;
            }
#else
            movePressed = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.05f;
            jumpPressed = Input.GetButton("Jump");
            sunPressed = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.05f;
            rewindPressed = rewindPressed || Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.LeftShift);
            echoPressed = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.C);
#endif

            SetPillState(pillMoveBg, movePressed, pillActiveColor);
            SetPillState(pillJumpBg, jumpPressed, pillActiveColor);
            SetPillState(pillSundialBg, sunPressed, colorDusk);
            SetPillState(pillRewindBg, rewindPressed, pillRewindActiveColor);
            SetPillState(pillEchoBg, echoPressed, colorZenith);
        }

        private void SetPillState(Image pillBg, bool active, Color activeCol)
        {
            if (pillBg == null) return;
            Color targetCol = active ? activeCol : pillNormalColor;
            pillBg.color = Color.Lerp(pillBg.color, targetCol, 20f * UnityEngine.Time.deltaTime);

            Vector3 targetScale = active ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
            pillBg.transform.localScale = Vector3.Lerp(pillBg.transform.localScale, targetScale, 20f * UnityEngine.Time.deltaTime);
        }

        private void UpdateSolarExposureVisuals()
        {
            if (solarDangerVignette != null)
            {
                Color c = solarDangerVignette.color;
                float targetAlpha = exposureFraction > 0.02f ? Mathf.Lerp(0.2f, 0.85f, exposureFraction) : 0f;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, 3.5f * UnityEngine.Time.deltaTime);
                solarDangerVignette.color = c;
            }

            if (solarAlertBanner != null)
            {
                bool showAlert = exposureFraction > 0.25f && !isDead;
                solarAlertBanner.SetActive(showAlert);

                if (showAlert && solarAlertText != null)
                {
                    float pulse = (Mathf.Sin(UnityEngine.Time.time * 20f) * 0.4f) + 0.6f;
                    solarAlertText.color = new Color(1f, 0.85f, 0.4f, pulse);
                }
            }
        }

        private void UpdateDeathOverlayVisuals()
        {
            if (deathOverlayGroup != null)
            {
                float targetAlpha = isDead ? 1.0f : 0.0f;
                deathOverlayGroup.alpha = Mathf.MoveTowards(deathOverlayGroup.alpha, targetAlpha, 4f * UnityEngine.Time.deltaTime);
                deathOverlayGroup.blocksRaycasts = isDead;

                if (isDead && deathPromptText != null)
                {
                    float pulse = (Mathf.Sin(UnityEngine.Time.unscaledTime * 14f) * 0.35f) + 0.65f;
                    deathPromptText.color = new Color(0.96f, 0.25f, 0.37f, pulse);
                }
            }
        }

        private void UpdateLevelTitleBanner()
        {
            if (levelTitleGroup == null) return;

            if (titleTimer > 0f)
            {
                titleTimer -= UnityEngine.Time.deltaTime;
                levelTitleGroup.alpha = Mathf.MoveTowards(levelTitleGroup.alpha, 1f, 3f * UnityEngine.Time.deltaTime);
            }
            else
            {
                levelTitleGroup.alpha = Mathf.MoveTowards(levelTitleGroup.alpha, 0f, 1.5f * UnityEngine.Time.deltaTime);
            }
        }

        public void ShowLevelTitle(string title, string subtitle, float duration = 3.0f)
        {
            if (levelTitleText != null) levelTitleText.text = title;
            if (levelSubtitleText != null) levelSubtitleText.text = subtitle;
            titleTimer = duration;
        }

        #endregion
    }
}
