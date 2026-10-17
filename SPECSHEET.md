# SPECSHEET.md — Umbra Chronos: Status & Task Plan

**This file = what is done, what is next, how to build it.**
Stable rules, architecture, and script contracts live in `AGENTS.md`. Read that first.

**Agents:** when you finish a task, update its status in the table below and tick its checklist box.
**Team:** Shaf (Mac), Chendline (Windows). **Deadline:** Sunday 09:30 AM PKT (submit by 09:15). All times are PKT and are targets.

---

## 1. Core Loop

World rule: light is lethal, shadows are solid.

1. Rotate the sun (`Q`/`E`/scroll) to cast a solid shadow bridge over a lethal light chasm.
2. Rewind (`R` / Left Shift) pulls the player and sun clock back along a 5 s buffer.
3. After rewinding, tap `F` / `C` to leave an Umbral Echo that replays your past run, holding plates and gates.

The 3-minute trailer decides the Top 5. Everything must look good on video.

---

## 2. Event Flow

All systems talk through `TimeEvents` (see `AGENTS.md`). Nothing references anything else directly.

- `SunDialController` (Q/E) → moves sun light → fires `OnSunAngleChanged(angle)`
  - `DynamicShadowCollider` listens → rebuilds the `EdgeCollider2D` shadow bridge.
- `RewindManager` (R/Shift) → fires `OnRewindStateChanged(bool)` → drives `TimeBody` on rewindable objects + VFX/audio.
- `PlayerController` records via `TimeBody`; on `F`/`C` it spawns a `UmbralEcho`.
- `UmbralEcho` replays the trajectory → triggers `PressurePlate` → opens `TimedGate`.
- `LethalLightZone` raycasts sun → player; exposure past the grace period fires `OnPlayerDied`.
- `GameManager` listens to `OnPlayerDied` → respawns the player and resets level state.

---

## 3. Status Table

Status: DONE / IN PROGRESS / READY / QUEUED

| Module | Owner | Target | Status |
| :--- | :--- | :--- | :--- |
| Git & architecture | Both | Sat 09:30 AM | DONE |
| Player controller | Shaf | Sat 10:00 AM | DONE |
| Rewind buffer (`TimeBody`, `RewindManager`) | Shaf | Sat 10:00 AM | DONE |
| Umbral Echo | Shaf | Sat 10:30 AM | DONE |
| `PressurePlate`, `TimedGate` | Shaf | Sat 10:30 AM | DONE |
| `SandboxBuilder.cs` (prefab + sandbox generator) | Shaf | — | DONE |
| `SunDialController` | Shaf | Sat 12:00 PM | DONE |
| `DynamicShadowCollider` | Shaf | Sat 02:00 PM | DONE |
| `LethalLightZone` | Shaf | Sat 01:00 PM | DONE |
| Rewind screen VFX (post-processing) | Shaf | Sat 02:00 PM | DONE |
| HUD + controls overlay | Shaf | Sat 07:00 PM | DONE |
| Atmospheric Godrays & Solar Particle VFX | Shaf | Sat 09:30 PM | DONE |
| Player Character Art & Animation ("Umbral Wanderer" + Echo) | Shaf | Sat 10:00 PM | DONE |
| Unified Master Level & Environment Contrast (`Level_Master.unity`) | Both | Sat 10:30 PM | DONE |
| Audio + `AudioManager` | Shaf | Sat 11:00 PM | DONE |
| Camera shake & particle polish | Shaf | Sat 11:30 PM | DONE |
| Playtesting + full playthrough | Both | Sun 12:00 AM – 02:00 AM | IN PROGRESS |
| Trailer + submission | Both | Sun 05:00 – 09:15 AM | QUEUED |

**Already built & merged to `main`:** Unity 6 URP 2D project, `TimeEvents`, snappy `PlayerController` (coyote, jump buffer, variable jump), 5 s circular rewind buffer, `UmbralEcho`, `PressurePlate` + `TimedGate`, `DynamicShadowCollider` horizontal shadow bridges, `LethalLightZone`, diegetic HUD (sundial compass, 3-tier non-overlapping rewind gauge, danger vignette, runic prompts), `GodraySystem` (roof-occluding light shafts, solar dust particles, player radiation embers, shadow alignment).

---

## 4. Phase 2: Core Mechanics & Systems (Completed)

All foundational mechanics are verified, decoupled via `TimeEvents`, and merged into `main`:
1. **`SunDialController.cs`**: `Q`/`E`/scroll orbits celestial light, triggers `OnSunAngleChanged`.
2. **`DynamicShadowCollider.cs`**: Rebuilds real-time solid `EdgeCollider2D` shadow bridges from monolith corners to ground.
3. **`LethalLightZone.cs`**: Raycasts sun to player with a strict 0.4s grace period, triggers `OnPlayerDied`.
4. **`RewindManager.cs` & `TimeBody.cs`**: 300-frame (5.0s) circular history buffer, dynamic chromatic aberration/vignette post-processing.
5. **`HUDController.cs` & `HUD.prefab`**: 3-tier non-overlapping rewind gauge, sundial compass needle, runic input overlay.

---

## 5. Phase 3: The Unified Contiguous Master Level (`Level_Master.unity`)

Instead of multiple fragmented level scenes with loading screens, the entire competition demo is built as **one contiguous, seamlessly streaming master level**: `Assets/_Project/Scenes/Levels/Level_Master.unity`.

The player journeys through **The Grand Sanctuary of Chronos**, split into 4 interconnected puzzle wings via grid-based screen-shift camera transitions:

### Wing 1: The Courtyard of Dawn (`Room_0_0`)
- **Theme & Mechanics:** Introduction to SunDial rotation (`Q`/`E`) and physical shadow bridging.
- **Layout:** Player starts on a cliff under an obsidian portico. Ahead lies an 8-unit lethal sunlit chasm with a tall monolith.
- **Solution:** Rotating the celestial sun from Zenith ($90^\circ$) to Dawn ($35^\circ$) casts an elongated shadow bridge across the void. The player walks across the solid shadow to reach the interior temple gate.

### Wing 2: The Echo Vault (`Room_1_0`)
- **Theme & Mechanics:** Introduction to Tape-Reel Rewind (`Hold R`) and Umbral Echo (`Tap F`).
- **Layout:** A sealed vertical stone portcullis blocks the exit corridor. A glowing runic pressure plate sits atop a high stone balcony requiring climbing ruins.
- **Solution:** Player climbs to the balcony, stands on the plate for 2 seconds (opening the gate below), holds `R` to rewind back down to the ground, and taps `F`. The Umbral Echo replays the climb and holds the plate while the player sprints through the open gate.

### Wing 3: The Clockwork Spire (`Room_2_0`)
- **Theme & Mechanics:** Real-time shadow pivoting + Echo coordination.
- **Layout:** A two-tier vertical chamber. An elevated switch on the left wall lowers an overhead barrier, but reaching the upper exit requires a shadow bridge angled to the right.
- **Solution:** Player rotates the sun to Dawn to cast a bridge to the left switch, steps on it, rewinds back, and spawns the Echo to hold it. While the Echo keeps the barrier lowered, the player rapidly pivots the sun to Dusk ($145^\circ$) to cast a rightward bridge and climbs up to the upper sanctuary.

### Wing 4: The Eclipse Paradox Sanctuary (`Room_3_0` — Climax)
- **Theme & Mechanics:** The grand dual-switch climax combining all mechanics under an ominous violet Eclipse sky.
- **Layout:** Two ancient glyph switches on opposing ledges flanking a massive central solar void. The final Sanctuary Archway requires both switches to be held concurrently.
- **Solution:** 
  1. Rotate sun to bridge across to Switch 1; hold it for 2s.
  2. Rewind back across the bridge; tap `F` to leave the Echo holding Switch 1.
  3. Immediately pivot the sun to the reciprocal angle, stretching the shadow bridge across to Switch 2.
  4. Sprint across to Switch 2 and stand on it simultaneously while the Echo holds Switch 1.
  5. The colossal Sanctuary Archway grinds open, leading to the Sanctuary Core and victory.

---

## 6. Strict Bulletproof Level Design Guidelines

Every chamber, puzzle, and platform in `Level_Master.unity` MUST adhere strictly to the following mathematical rules. Any level design violating these rules will break game feel, cause player softlocks, or ruin the 3-minute video presentation.

### Rule 1: Movement & Jump Metrics (Ground Truths)
- **Horizontal Run Speed:** Exactly `8.0` units/sec.
- **Jump Peak Height:** Maximum theoretical peak is `3.2` units. **Maximum safe vertical jump reach is `2.8` units.** Never place a jump ledge higher than $2.8$ units above the floor without intermediate stepping stones.
- **Horizontal Jump Reach:** Sprint jump maximum span is `5.5` units. **Maximum safe horizontal gap is `4.2` units.**
- **Headroom Clearance:** Every tunnel, doorway, or under-platform passage must have at least **`2.4` units vertical clearance** (player collider is $1.8\text{ u}$ high).
- **Platform Edge Thickness:** Floor slabs and floating platforms must be at least **`0.6` units thick** to prevent collision tunneling during high-velocity downward falls.

### Rule 2: SunDial & Shadow Geometry Rules
- **Sun Orbit Center:** The SunDial orbit center must align with the horizontal midpoint of the chamber's lethal chasm. Orbit radius must be between **`14.0` and `16.0` units**.
- **Sun Angle Arc:** Clamped strictly between **`15.0°` (Dawn)** and **`165.0°` (Dusk)**. Zenith is $90.0^\circ$.
- **Monolith Proportions:** Monoliths must be between **`5.0` and `8.0` units tall** and **`1.8` to `2.4` units wide**. Their base must be embedded directly into solid obsidian ground (`Layer: Ground`).
- **Ground Receptor Rule:** Every lethal chasm receiving a shadow bridge MUST have a defined horizontal floor plane or receiving platform with a consistent Y coordinate. `DynamicShadowCollider` raycasts downward; casting shadows into an infinite open void without a receptor will produce non-functional or degenerate colliders.
- **Flush Edge Transition:** The landing point of the shadow bridge must rest flush with or slightly overlap ($\le 0.15\text{ u}$) the receiving ledge so the player transitions smoothly between shadow and stone without catching on vertical collider lips.

### Rule 3: Lethal Radiation & Shelter Architecture
- **The 0.4s Grace Threshold:** The player is vaporized if exposed to direct sunlight for $> 0.4\text{ s}$. At $8.0\text{ u/s}$ run speed, maximum unprotected sprint distance before death is $3.2$ units. **Never design any open sunlit gap $> 2.0$ units without an active shadow bridge.**
- **Contemplation Sanctuaries (Safe Shadow Pockets):** Every puzzle wing MUST feature an obsidian ceiling, archway, or permanent monolith shadow where the player can safely stand indefinitely while studying the room and turning the sundial. Puzzles must test ingenuity, not frantic time limits.
- **Zero-Damage Transition Thresholds:** The entry threshold and exit corridor of every chamber MUST be 100% inside a safe shadow zone. A screen-shift transition must NEVER spawn or drop the player into lethal light.

### Rule 4: Rewind & Umbral Echo Timing Budget
- **Circular Buffer Capacity:** Exactly **`5.0` seconds** ($300$ frames at $60\text{ Hz}$).
- **The 3.5s Execution Rule:** The sequence of actions assigned to the Echo (e.g. running from the door, climbing a ledge, stepping onto a pressure plate) must take **no longer than `3.5` seconds**. This guarantees that when the player holds `R`, they have at least **`1.5` seconds of buffer remaining** to rewind back through the gate threshold before deploying the Echo.
- **Plate Hold Duration:** The player must stand on the target pressure plate for at least **`1.5` to `2.0` seconds** in recorded history before hitting rewind, giving the player ample forward time to cross the gate while the Echo holds it.
- **Softlock Elimination:** Timed gates must slide upward into the ceiling. If a player bypasses a gate, the interior chamber must either provide an internal bypass switch or allow rewinding backward out of the gate. Players must NEVER be permanently trapped.

### Rule 5: Screen-Shift Camera Grid & Viewport Framing
- **Viewport Aspect Ratio:** Fixed $16:9$ orthographic framing (`orthographicSize = 7.5` to `8.0`).
- **Room Dimensions:** Every chamber must fit neatly within a standard grid cell ($26.0\text{ u}$ width $\times$ $15.0\text{ u}$ height).
- **Zero Blind Jumps:** All active puzzle components in a chamber—the SunDial position, Monolith, Pressure Plate, Timed Gate, and Target Exit—**MUST be completely visible within the camera's single screen frame**. Never force the player to make a blind jump or interact with off-screen mechanisms.
- **Camera Shift Dampening:** Screen transitions must trigger cleanly when the player crosses the chamber threshold ($X$ or $Y$ boundary), smoothly shifting to the next room grid over `0.35` seconds.

### Rule 6: Diegetic Visual Contrast ("Ink & Solar Flare")
- **Foreground Colliders:** Pure Obsidian Black (`#0D0D15`). All walkable ground, monoliths, and structural columns must be 100% opaque black silhouettes.
- **Interactable Elements:** Pressure plates and gate runes must be muted slate (`#1E293B`) when inactive, snapping to brilliant radiant cyan (`#38BDF8`) when energized.
- **Lethal Radiation Areas:** Blinding solar yellow-white (`#FFF8E7` to `#FFD166`) with high URP Bloom glare.
- **Umbral Echo Ghost:** Deep midnight indigo (`#1E1B4B`) with a glowing `70%` alpha cyan rim (`#38BDF8`).

---

## 7. Phase 4: Audio, Polish, & Camera Shake

1. **`AudioManager.cs`:**
   - SFX: `sfx_tick.wav` (sun rotating ratchet), `sfx_tape_rewind.wav` (looped rewind whir), `sfx_plate_click.wav`, `sfx_gate_open.wav`, `sfx_vaporize.wav` (solar death), `sfx_echo_spawn.wav`, `sfx_jump.wav`.
   - Dynamic Rewind Pitch-Shift: Lerp audio master/music pitch down to `0.5x` with tape wobble while rewinding.
2. **Camera Shake & Environmental VFX:**
   - Screen shake on lethal solar damage, gate grinding, and heavy plate triggering in `CameraController.cs`.
   - Micro-particles: Obsidian dust on jump/landing, solar embers in lethal chasms, scanline pulse on Echo deployment.
3. **Death & Rewind Recovery:**
   - On death: flash monochrome vignette with prompt `[HOLD R TO REWIND TIME]`. Player can rewind time backwards to undo their death! If buffer is empty, auto-respawn at current chamber entry.

---

## 8. Phase 5: Trailer & Submission Plan (Exactly 3:00)

The 3-minute video decides the Top 5 shortlisting:

| Time | Segment | Content | Audio |
| :--- | :--- | :--- | :--- |
| **0:00–0:30** | The Hook | Title card *"Umbra Chronos: Echoes of the Sundial"* + *"Light is Lethal. Shadows are Matter."* Blinding sun overlooking the ancient clocktower abyss. | Low atmospheric drone + clock escapement tick |
| **0:30–1:15** | Wing 1 & 2 Gameplay | Raw uncut gameplay: Rotating the sun, walking on the solid shadow bridge across the Dawn chasm. Scaling the Echo Vault, rewinding time, spawning the cyan-rimmed Echo to hold the gate. | Rhythmic percussion + tape rewind whir |
| **1:15–2:15** | Wing 3 & 4 Gameplay | Raw uncut gameplay: Real-time shadow pivoting in the Clockwork Spire. The grand dual-switch climax in the Eclipse Paradox under violet skies. | Dynamic synth crescendo into harmonic resolution |
| **2:15–3:00** | Mechanics Breakdown & Outro | Concise presentation explaining the synergy of Time (Rewind + Echoes) and Shadow (Solid Light-Matter Physics). Credits & NaSCon '26 badge. | Resolving ambient piano chord |

### Submission Checklist (Sunday 09:15 AM PKT):
- [ ] Contiguous playthrough recorded at 1080p60 (macOS screen recorder / OBS).
- [ ] 3-minute trailer edited with title cards and audio sync.
- [ ] Video uploaded to Google Drive ("Anyone with the link can view").
- [ ] Standalone builds generated: macOS (`StandaloneOSX`) and Windows (`StandaloneWindows64`).
- [ ] Clean GitHub repository with gameplay GIFs, documentation, and source code.
- [ ] Final submission submitted on the NaSCon portal.