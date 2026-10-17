# ⏱️ Umbra Chronos: Master Execution Roadmap & Specsheet
**NaSCon '26 Game Dev | 24-Hour Parallel Development Blueprint**  
**Team:** Shaf (Mac) & Chendline (Windows)  
**Submission Deadline:** Sunday 09:30 AM PKT (~23 Hours Remaining)  

---

## 🎯 Executive Summary & The Core Game Loop

* **Core World Rule:** **Celestial light is lethal radiation; Shadows are physical, solid matter.**
* **The Gameplay Loop:**
  1. The player navigates lethal chasms of light by rotating the Sun (`Q`/`E` or Mouse Scroll) to cast solid shadow bridges.
  2. Rewinding time (`R` / Left Shift) pulls player physics and the celestial clock backward along a 5-second tape buffer.
  3. Snapping back in time leaves behind an **Umbral Echo** (`F` / `C` after rewind), a past-self shadow clone that holds pressure plates, triggers gates, or provides an extra stepping stone.
* **Evaluation Truth:** Initial evaluation and the **Top 5 finalist shortlisting is judged SOLELY on the 3-minute video trailer**. Gameplay must look punchy, fluid, and visually stunning on video.

---

## 🔄 Component Relationship & Data Flow Architecture

Here is how all systems connect and pass events across the project without tight coupling:

```
                          ┌───────────────────────────┐
                          │   TimeEvents (Event Bus)  │
                          └─────────────┬─────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           │ OnSunAngleChanged(float)   │ OnRewindStateChanged(bool) │ OnPlayerDied()
           ▼                            ▼                            ▼
┌───────────────────────┐   ┌────────────────────────┐   ┌────────────────────────┐
│  SunDialController    │   │     RewindManager      │   │      GameManager       │
│  (Chendline)          │   │     (Shaf)             │   │      (Shaf/Chendline)  │
│  • Listens to Q/E     │   │  • Listens to [R]/Shift│   │  • Respawns Player     │
│  • Orbit PointLight2D │   │  • Drives TimeBody.cs  │   │  • Resets Level State  │
│  • Broadcasts angle   │   │  • Controls VFX/Audio  │   └────────────────────────┘
└──────────┬────────────┘   └───────────┬────────────┘
           │                            │
           ▼                            ▼
┌───────────────────────┐   ┌────────────────────────┐
│ DynamicShadowCollider │   │   PlayerController     │
│ (Chendline)           │   │   (Shaf)               │
│ • Reads Sun position  │   │  • Snappy 2D Platformer│
│ • Projects monolith   │   │  • TimeBody recorder   │
│   shadow to ground    │   │  • Coyote & Buffer     │
│ • Updates EdgeCollider│   └───────────┬────────────┘
│   (Solid bridge!)     │               │
└───────────────────────┘               │ Spawns on [F]
                                        ▼
┌───────────────────────┐   ┌────────────────────────┐
│   LethalLightZone     │   │      UmbralEcho        │
│   (Shaf)              │   │      (Shaf)            │
│ • Raycasts Sun->Player│   │  • Replays trajectory  │
│ • If exposed > 0.4s:  │   │  • Triggers plates     │
│   fires OnPlayerDied  │   │  • Ignores Player col  │
└───────────────────────┘   └───────────┬────────────┘
                                        │ Steps on
                                        ▼
                            ┌────────────────────────┐   Activates   ┌────────────────────────┐
                            │     PressurePlate      │──────────────▶│       TimedGate        │
                            │  • Trigger OverlapBox  │               │  • Smooth vertical     │
                            │  • Zero-jitter sensor  │               │    stone sliding door  │
                            └────────────────────────┘               └────────────────────────┘
```

---

## 👥 Parallel Ownership Breakdown

| Module | Primary Owner | Target Delivery Time | Status |
| :--- | :--- | :--- | :--- |
| **Git & Architecture** | Shaf & Chendline | Saturday 09:30 AM | ✅ **DONE** |
| **Player Controller (Platformer Juice)** | Shaf | Saturday 10:00 AM | ✅ **DONE** |
| **Rewind Buffer (`TimeBody` & `RewindManager`)** | Shaf | Saturday 10:00 AM | ✅ **DONE** |
| **Umbral Echo (Past Ghost Recording)** | Shaf | Saturday 10:30 AM | ✅ **DONE** |
| **Puzzle Primitives (`PressurePlate`, `TimedGate`)** | Shaf | Saturday 10:30 AM | ✅ **DONE** |
| **SunDial Orbit & Input (`SunDialController`)** | Chendline | Saturday 12:00 PM | 🟡 **IN PROGRESS** |
| **Dynamic Shadow Collider (`DynamicShadowCollider`)** | Chendline | Saturday 02:00 PM | 🟡 **IN PROGRESS** |
| **Lethal Sunlight Raycast (`LethalLightZone`)** | Shaf | Saturday 01:00 PM | 🟡 **READY TO START** |
| **Rewind Screen VFX (Post-Processing Volume)** | Shaf | Saturday 02:00 PM | 🟡 **READY TO START** |
| **Level 01: "Dawn of the Sundial"** | Chendline | Saturday 04:00 PM | ⚪ Queued |
| **Level 02: "Echo of the Vault"** | Chendline | Saturday 05:30 PM | ⚪ Queued |
| **Level 03: "The Eclipse Paradox"** | Chendline | Saturday 07:00 PM | ⚪ Queued |
| **Audio Sourcing & Pitch-Shift SFX** | Shaf | Saturday 05:00 PM | ⚪ Queued |
| **HUD & Controls UI Overlay** | Shaf | Saturday 07:00 PM | ⚪ Queued |
| **Full Playtesting & Level Polish** | Both | Saturday 08:00 PM – 02:00 AM | ⚪ Queued |
| **3-Minute Video Trailer & Submission** | Both | Sunday 05:00 AM – 09:00 AM | ⚪ Queued |

---

## 📋 Master Chronological Step-by-Step Checklist

### 🟢 PHASE 1: Core Foundation (COMPLETED)
- [x] Unity 6 (`6000.6.6f1`) URP 2D project initialized and synced.
- [x] Global event bus: `TimeEvents.cs` (`OnSunAngleChanged`, `OnRewindStateChanged`, `OnEchoCreated`, `OnPlayerDied`).
- [x] Snappy Player Controller: Coyote time (`0.15s`), jump buffer (`0.12s`), variable jump cut, ground check (`PlayerController.cs`).
- [x] 5-second circular buffer rewind: `TimeBody.cs` and `RewindManager.cs`.
- [x] Umbral Echo: Cloned silhouette replaying past movements, collision disabled with player (`UmbralEcho.cs`).
- [x] Pressure Plate & Gate: Smooth trigger-based sensor preventing jitter or edge snagging (`PressurePlate.cs`, `TimedGate.cs`).
- [x] Automated Sandbox & Prefab Generator: `SandboxBuilder.cs` generating prefabs and `Sandbox_Shaf.unity`.

---

### 🟡 PHASE 2: Saturday Morning (10:00 AM – 2:00 PM) — "Celestial Shadows & Danger"
*Goal: Turn the sun, make cast shadows solid walkable platforms, and make unshaded light lethal.*

#### 👨‍💻 Chendline's Step-by-Step (Tell Chendline to do this right now):

1. **Step C1: Sync & Open Sandbox**
   * Run `git pull origin main` in terminal.
   * Open `UmbraChronos` in Unity Editor.
   * Work strictly inside `Assets/_Project/Scenes/Sandboxes/Sandbox_Chendline.unity` (create if not present).

2. **Step C2: Implement `SunDialController.cs`**
   * **File:** `Assets/_Project/Scripts/Environment/SunDialController.cs`
   * **Namespace:** `UmbraChronos.Environment`
   * **Inspector Fields:**
     * `[SerializeField] private Light2D sunLight;` (Reference to scene's primary Point Light 2D).
     * `[SerializeField] private Transform orbitCenter;` (Center point of the level, e.g. `Vector2.zero`).
     * `[SerializeField] private float orbitRadius = 14f;`
     * `[SerializeField] private float rotationSpeed = 60f;` (degrees per second).
     * `[SerializeField] private float minAngle = 15f;`
     * `[SerializeField] private float maxAngle = 165f;`
   * **Behavior:**
     * In `Update()`: Read horizontal input or `Q` (-1) / `E` (+1) / Mouse Scroll wheel.
     * Calculate `currentAngle = Mathf.Clamp(currentAngle + input * rotationSpeed * Time.deltaTime, minAngle, maxAngle);`.
     * Update light position:
       ```csharp
       float rad = currentAngle * Mathf.Deg2Rad;
       Vector3 newPos = orbitCenter.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
       sunLight.transform.position = newPos;
       ```
     * Broadcast: `TimeEvents.OnSunAngleChanged?.Invoke(currentAngle);` whenever the angle changes.

3. **Step C3: Implement `DynamicShadowCollider.cs` (Solid Shadows)**
   * **File:** `Assets/_Project/Scripts/Environment/DynamicShadowCollider.cs`
   * **Namespace:** `UmbraChronos.Environment`
   * **Components Required:** `EdgeCollider2D` or `PolygonCollider2D` on the Monolith GameObject.
   * **Behavior & Algorithm:**
     * Subscribe to `TimeEvents.OnSunAngleChanged`.
     * Identify the top-left and top-right corners of the monolith sprite.
     * For each corner vertex $V$, cast a ray from the Sun position $S$ through $V$ to find where the shadow hits the ground ($Y = groundY$ or `Physics2D.Raycast(V, (V - S).normalized, 30f, groundLayer)`).
     * Update the points of the `EdgeCollider2D` along the top perimeter:
       `points = [ LeftShadowEdgeGround, TopLeftCorner, TopRightCorner, RightShadowEdgeGround ]`.
     * Now, when the player lands on the cast shadow, Unity 2D physics treats it as solid ground!

4. **Step C4: Verify in `Sandbox_Chendline.unity`**
   * Place a chasm with lethal ground below.
   * Place a monolith in the center.
   * Press `Q`/`E`: watch the shadow stretch across the chasm.
   * Run the player onto the shadow bridge and walk across!

---

#### 👨‍💻 Shaf's Step-by-Step (Parallel):

1. **Step S1: Implement `LethalLightZone.cs` (The Danger Mechanic)**
   * **File:** `Assets/_Project/Scripts/Environment/LethalLightZone.cs`
   * **Namespace:** `UmbraChronos.Environment`
   * **Inspector Fields:**
     * `[SerializeField] private Transform sunTransform;`
     * `[SerializeField] private LayerMask shadowBlockerMask;` (Monoliths / Shadow Casters).
     * `[SerializeField] private float lethalGracePeriod = 0.4f;`
   * **Behavior:**
     * In `FixedUpdate()`: Raycast from `sunTransform.position` to `player.position`.
     * If ray hits player directly (no blocker in between):
       * Start a warning timer. Screen edges glow searing white.
       * If exposed for $> 0.4\text{s}$, invoke `TimeEvents.OnPlayerDied?.Invoke()`.
     * If blocked by a monolith: Player is in shadow &rarr; safe! Reset timer.

2. **Step S2: URP Post-Processing Rewind VFX**
   * Create a Global Volume in the scene.
   * Add **Chromatic Aberration** and **Vignette**.
   * In `RewindManager.cs`: Hook into `TimeEvents.OnRewindStateChanged(bool isRewinding)`.
   * When rewinding: Lerp Chromatic Aberration to `0.9f` and Vignette to `0.45f` with a retro scanline effect.
   * When released: Snap back smoothly to `0f`.

---

### 🟠 PHASE 3: Saturday Afternoon (2:00 PM – 7:00 PM) — "The 3 Showcase Levels"
*Goal: Assemble the 3 progressive showcase levels for the video trailer.*

#### 👨‍💻 Chendline's Tasks (Level Authoring):
> *Note: Chendline creates these scenes directly in `Assets/_Project/Scenes/Levels/`. Because Chendline is the sole author of these scenes, there are zero git conflicts!*

1. **Level 01: "Dawn of the Sundial" (`Level_01.unity`)**
   * **Mechanic Highlight:** Sun rotation & shadow bridge.
   * **Walkthrough:**
     - Player stands on starting cliff.
     - 8-unit wide lethal chasm in front.
     - Tall monolith stands in the chasm.
     - Player presses `E` to rotate Sun toward the horizon &rarr; shadow stretches out &rarr; forms a bridge &rarr; player walks across into the Sanctuary Door.
   * **Estimated Build Time:** 1.5 hours.

2. **Level 02: "Echo of the Vault" (`Level_02.unity`)**
   * **Mechanic Highlight:** Rewind & Umbral Echo switch puzzle.
   * **Walkthrough:**
     - Player enters a chamber with a closed stone gate (`TimedGate`).
     - A pressure plate is high up on a platform or behind a temporary barrier.
     - Player climbs to the plate, stands on it for 2 seconds (gate opens across the room).
     - Player holds `[R]` to rewind back to the entrance door.
     - Player taps `[F]` to spawn the **Umbral Echo**.
     - The Echo replays climbing and standing on the plate.
     - While the Echo holds the gate open, the player sprints through the gate!
   * **Estimated Build Time:** 1.5 hours.

3. **Level 03: "The Eclipse Paradox" (`Level_03.unity`)**
   * **Mechanic Highlight:** Multi-Theme Climax (SunDial + Solid Shadow + Echo + Timed Gate).
   * **Walkthrough:**
     - Two switches must be held simultaneously to open the final seal.
     - Switch 1 is across a lethal sunlit chasm.
     - Player turns Sun to cast shadow bridge &rarr; crosses to Switch 1.
     - Steps on Switch 1 &rarr; rewinds back &rarr; spawns Umbral Echo to hold Switch 1.
     - Player turns the Sun to the opposite angle &rarr; casts a second shadow bridge to Switch 2.
     - Player stands on Switch 2 while Echo holds Switch 1 &rarr; Sanctuary Arch opens!
   * **Estimated Build Time:** 2 hours.

#### 👨‍💻 Shaf's Tasks (Parallel):
1. **Task S3: Audio Design & Sound Management**
   * Download 5 crisp CC0 sound effects:
     * `sfx_tick.wav`: Crisp clock mechanical tick when rotating the Sundial.
     * `sfx_tape_rewind.wav`: Reverse tape whoosh played while holding `[R]`.
     * `sfx_plate_click.wav`: Heavy stone click on pressure plate activation.
     * `sfx_gate_open.wav`: Ancient stone grind when gate raises.
     * `sfx_vaporize.wav`: Searing burn sizzle on lethal light exposure.
     * `bgm_ambient.mp3`: Haunting, minimalist piano/synth drone.
   * Create `AudioManager.cs` to trigger SFX on `TimeEvents`.

2. **Task S4: Polished HUD & UI Overlay**
   * **Sundial Compass (Top-Right):** Circular minimalist dial showing sun orientation and time of day (Dawn / Noon / Dusk / Eclipse).
   * **Rewind Energy Gauge:** Clean glowing stamina bar showing remaining rewind tape duration (0 to 5 seconds).
   * **Key Prompt Tooltips:** Modern pill-badge prompts (`[A/D] Move`, `[Space] Jump`, `[Q/E] Rotate Sun`, `[Hold R / Shift] Rewind`, `[Tap F] Echo`).

---

### 🔵 PHASE 4: Saturday Night (8:00 PM – 2:00 AM) — "Polish, Juice & Playtesting"
*Goal: Turn functional prototypes into a captivating commercial-grade vertical slice.*

1. **Camera Shake & Impact Juice:**
   * Add subtle Cinemachine / 2D camera shake on gate opening and lethal light exposure.
2. **Particle Effects:**
   * Shadow dust particles when the player jumps or slides on solid shadows.
   * Rising embers/solar flares in lethal light zones.
   * Glitch scanline particles around the Umbral Echo clone.
3. **Seamless Death & Rewind Flow:**
   * When dying to light, screen briefly flashes monochrome; a prompt reads: `[HOLD R TO REWIND TIME]`.
   * The player can literally rewind out of death! If rewind buffer is empty, instant respawn at checkpoint.
4. **End-to-End Playtesting:**
   * Both developers run through `Level_01` &rarr; `Level_02` &rarr; `Level_03` without dev cheats.
   * Verify all physics feel responsive on both macOS and Windows.

---

### 🔴 PHASE 5: Sunday Morning (5:00 AM – 9:00 AM) — "Trailer Production & Submission"
*Goal: Lock in the Top 5 finalist shortlisting.*

#### 🎬 The 3-Minute Trailer Plan (Strict Rule):
| Timestamp | Segment Title | Description | Audio / Music |
| :--- | :--- | :--- | :--- |
| **0:00 – 0:30** | **The Hook** | High-contrast cinematic title: *"Umbra Chronos: Echoes of the Sundial"*. Title card: *"Light is Lethal. Shadow is Matter."* Show the blinding sun and a dark abyss. | Deep ambient drone + clock ticking |
| **0:30 – 1:30** | **The Sundial (Gameplay)** | Uncut gameplay of Level 01: rotating the celestial light, the shadow elongating across the void, the player stepping onto the solid shadow bridge and running across. | Subtle rhythmic percussion begins |
| **1:30 – 2:30** | **The Echo (Gameplay)** | Level 02 & 03 gameplay: running to a switch, rewinding time with retro chromatic aberration, spawning the cyan-rimmed Umbral Echo, and dual-solving the final gate. | Reverse tape-stop SFX into full crescendo |
| **2:30 – 3:00** | **Theme Synthesis & Outro** | Explaining the dual theme: **Time** (Rewind + Echoes) and **Shadow** (Solid light-matter physics). Team credits & NaSCon '26 badge. | Resolving piano chord |

#### 📦 Final Submission Checklist (Cutoff: Sunday 09:30 AM PKT):
- [ ] Record 1080p60 gameplay footage using OBS Studio or Mac screen recorder.
- [ ] Edit the 3-minute video (CapCut, Premiere, or DaVinci Resolve) with clean title cards.
- [ ] Export video and upload to Google Drive: **Set link sharing to "Anyone with the link can view"**.
- [ ] Generate standalone builds:
  - macOS: `unity build UmbraChronos --editor-version 6000.6.6f1 --target StandaloneOSX`
  - Windows: `unity build UmbraChronos --editor-version 6000.6.6f1 --target StandaloneWindows64`
- [ ] Ensure public GitHub repository has clean `README.md`, gameplay GIFs, and full code.
- [ ] Submit drive link, repo link, and build to NaSCon portal before 09:15 AM (15-min safety margin!).

---

## 💬 Copy-Paste Messages for Chendline

### 📨 Message 1: Kickoff & Immediate Task (Send to Chendline Now!)
```
Hey Chendline! We have our core player movement, 5-second rewind buffer, and shadow clone (Umbral Echo) working and tested! 

Here is what we need you to tackle right now on your Windows machine:
1. Pull the latest from main: `git pull origin main`
2. Open the project in Unity 6 and work inside your sandbox: `Assets/_Project/Scenes/Sandboxes/Sandbox_Chendline.unity`
3. We need your two shadow mechanics:
   • SunDialController.cs: Rotates the scene's 2D light source using Q/E or mouse scroll and calls TimeEvents.OnSunAngleChanged(angle).
   • DynamicShadowCollider.cs: Put this on a stone monolith so that as the sun rotates, it calculates the projected shadow onto the ground and updates an EdgeCollider2D — this makes the shadow act as a solid walkable bridge!

While you build the Sun & Shadow colliders, I am coding the lethal sunlight raycasts (LethalLightZone.cs) and setting up the rewind post-processing VFX. Check SPECSHEET.md in the repo root for the full schedule and math hints! Let's crush this!
```

### 📨 Message 2: Level Authoring (Send to Chendline for Phase 3)
```
Hey Chendline! Awesome work on the shadow colliders. Now we move to Phase 3: assembling the 3 showcase puzzle levels for our 3-minute trailer!

Since Git can't merge scene files, you have 100% ownership of the level scenes in `Assets/_Project/Scenes/Levels/`:
1. Level_01 ("Dawn of the Sundial"): Teaches turning the sun with Q/E to stretch a shadow bridge across a lethal light chasm.
2. Level_02 ("Echo of the Vault"): Teaches running to a pressure plate, rewinding with [R], tapping [F] to leave an Umbral Echo holding the switch, and sprinting through the opened TimedGate.
3. Level_03 ("The Eclipse Paradox"): Climax puzzle requiring both a shadow bridge crossing and an echo holding a switch simultaneously.

All prefabs (Player, PressurePlate, TimedGate, PushableBlock) are ready in `Assets/_Project/Prefabs/`. Let me know if you need any adjustments to the prefabs!
```
