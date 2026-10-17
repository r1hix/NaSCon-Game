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
| `SunDialController` | Chendline / Shaf | Sat 12:00 PM | DONE |
| `DynamicShadowCollider` | Chendline | Sat 02:00 PM | IN PROGRESS |
| `LethalLightZone` | Shaf | Sat 01:00 PM | DONE |
| Rewind screen VFX (post-processing) | Shaf | Sat 02:00 PM | DONE |
| Level 01 "Dawn of the Sundial" | Chendline | Sat 04:00 PM | QUEUED |
| Level 02 "Echo of the Vault" | Chendline | Sat 05:30 PM | QUEUED |
| Level 03 "The Eclipse Paradox" | Chendline | Sat 07:00 PM | QUEUED |
| Audio + `AudioManager` | Shaf | Sat 05:00 PM | QUEUED |
| HUD + controls overlay | Shaf | Sat 07:00 PM | QUEUED |
| Playtesting + polish | Both | Sat 08:00 PM – Sun 02:00 AM | QUEUED |
| Trailer + submission | Both | Sun 05:00 – 09:15 AM | QUEUED |

**Already built (Phase 1):** Unity 6 URP 2D project, `TimeEvents`, snappy `PlayerController` (coyote, jump buffer, variable jump), 5 s rewind buffer, `UmbralEcho`, `PressurePlate` + `TimedGate`, `SandboxBuilder.cs`, `Sandbox_Shaf.unity`.

---

## 4. Phase 2 (now → Sat 2:00 PM): Shadows & Danger

Goal: sun rotates, shadows are solid, unshaded light kills.

### Chendline

**C1. Sync and open**
- `git pull --rebase origin main`, then create a branch (see `AGENTS.md` Rule 4).
- Open the project in Unity. Work only in `Assets/_Project/Scenes/Sandboxes/Sandbox_Chendline.unity` (create it if missing).

**C2. `SunDialController.cs`** — `Assets/_Project/Scripts/Environment/`, namespace `UmbraChronos.Environment`
- Fields (`[SerializeField] private`):
  - `Light2D sunLight` (the scene's Point Light 2D)
  - `Transform orbitCenter`
  - `float orbitRadius = 14f`
  - `float rotationSpeed = 60f` (deg/sec)
  - `float minAngle = 15f`, `float maxAngle = 165f`
- In `Update()`: read `Q` (−1), `E` (+1), or mouse scroll. Then:
  ```csharp
  currentAngle = Mathf.Clamp(currentAngle + input * rotationSpeed * UnityEngine.Time.deltaTime, minAngle, maxAngle);
  float rad = currentAngle * Mathf.Deg2Rad;
  sunLight.transform.position = orbitCenter.position
      + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
  ```
- Fire `TimeEvents.OnSunAngleChanged?.Invoke(currentAngle)` whenever the angle changes.

**C3. `DynamicShadowCollider.cs`** — same folder and namespace
- Goes on the monolith. Needs `EdgeCollider2D` (or `PolygonCollider2D`).
- Subscribe to `TimeEvents.OnSunAngleChanged` (unsubscribe in `OnDisable`).
- For the monolith's top-left and top-right corners, cast a ray from sun position `S` through corner `V` to the ground (`groundY`, or `Physics2D.Raycast(V, (V - S).normalized, 30f, groundLayer)`).
- Set the collider points to:
  `[LeftShadowEdgeGround, TopLeftCorner, TopRightCorner, RightShadowEdgeGround]`
- Result: the player can stand and walk on the cast shadow as solid ground.

**C4. Verify in sandbox**
- Build a lethal chasm with a monolith in the middle.
- Press `Q`/`E` and check the shadow stretches across the chasm.
- Walk the player across the shadow bridge. Check there is no jitter or falling through.

### Shaf

**S1. `LethalLightZone.cs`** — `Scripts/Environment/`, namespace `UmbraChronos.Environment`
- Fields: `Transform sunTransform`, `LayerMask shadowBlockerMask`, `float lethalGracePeriod = 0.4f`.
- In `FixedUpdate()`: raycast from the sun to the player.
  - Clear line to player: start the warning timer and glow the screen edges white. Past 0.4 s, fire `TimeEvents.OnPlayerDied?.Invoke()`.
  - Blocked by a monolith: player is in shadow, reset the timer.

**S2. Rewind VFX (URP)**
- Add a Global Volume with Chromatic Aberration and Vignette.
- In `RewindManager`, hook `TimeEvents.OnRewindStateChanged`.
  - Rewinding: lerp Chromatic Aberration to 0.9 and Vignette to 0.45, add a retro scanline effect.
  - Released: ease back to 0.

---

## 5. Phase 3 (Sat 2:00 – 7:00 PM): Three Showcase Levels

### Chendline — level authoring
Scenes go in `Assets/_Project/Scenes/Levels/`. Chendline is the only author, so no merge conflicts. Use prefabs from `Assets/_Project/Prefabs/` (Player, PressurePlate, TimedGate, PushableBlock).

**Level 01 "Dawn of the Sundial"** (`Level_01.unity`, ~1.5 h)
- Teaches: sun rotation + shadow bridge.
- Player starts on a cliff. An 8-unit lethal light chasm lies ahead with a tall monolith in it.
- Player presses `E` → shadow stretches out as a bridge → player crosses to the Sanctuary Door.

**Level 02 "Echo of the Vault"** (`Level_02.unity`, ~1.5 h)
- Teaches: rewind + Echo + pressure plate.
- Closed `TimedGate` in a chamber. The plate is high up or behind a barrier.
- Player climbs to the plate and stands on it ~2 s (gate opens across the room).
- Hold `R` to rewind to the entrance, tap `F` to spawn the Echo.
- Echo replays the climb and holds the plate. Player sprints through the open gate.

**Level 03 "The Eclipse Paradox"** (`Level_03.unity`, ~2 h)
- Climax: sundial + solid shadow + Echo + timed gate.
- Two switches must be held at the same time to open the final seal.
- Switch 1 is across a lethal chasm: rotate sun → shadow bridge → cross → stand on Switch 1 → rewind → spawn Echo to hold it.
- Rotate sun to the opposite angle → second shadow bridge to Switch 2.
- Player stands on Switch 2 while the Echo holds Switch 1 → Sanctuary Arch opens.

### Shaf — audio and HUD

**S3. Audio.** Download CC0 files into `Audio/SFX` and `Audio/Music`:
- `sfx_tick.wav` (sun rotating), `sfx_tape_rewind.wav` (while holding R), `sfx_plate_click.wav`, `sfx_gate_open.wav`, `sfx_vaporize.wav` (lethal light), `bgm_ambient.mp3` (minimal piano/synth drone).
- Write `AudioManager.cs` that plays SFX from `TimeEvents`. Pitch audio down during rewind.

**S4. HUD.**
- Sundial compass (top-right): sun orientation + time of day (Dawn / Noon / Dusk / Eclipse).
- Rewind energy gauge: remaining tape, 0–5 s.
- Key prompt pills: `[A/D] Move`, `[Space] Jump`, `[Q/E] Rotate Sun`, `[Hold R / Shift] Rewind`, `[Tap F] Echo`.

---

## 6. Phase 4 (Sat 8:00 PM – Sun 2:00 AM): Polish and Playtesting

1. **Camera shake** (Cinemachine or simple 2D shake) on gate open and lethal light exposure.
2. **Particles:** shadow dust on jump/slide, embers in lethal zones, glitch scanlines around the Echo.
3. **Death flow:** on death the screen flashes monochrome and shows `[HOLD R TO REWIND TIME]`. The player can rewind out of death. If the buffer is empty, respawn at the checkpoint.
4. **Playtest:** both devs play Level 01 → 02 → 03 with no dev cheats. Check that physics feel the same on macOS and Windows.

---

## 7. Phase 5 (Sun 5:00 – 9:15 AM): Trailer and Submission

### Trailer plan (exactly 3:00)

| Time | Segment | Content | Audio |
| :--- | :--- | :--- | :--- |
| 0:00–0:30 | Hook | Title card "Umbra Chronos: Echoes of the Sundial" + "Light is Lethal. Shadow is Matter." Blinding sun over a dark abyss. | Deep drone + clock ticking |
| 0:30–1:30 | Sundial (gameplay) | Uncut Level 01: rotate sun, shadow stretches, player runs across the bridge. | Light rhythmic percussion |
| 1:30–2:30 | Echo (gameplay) | Levels 02 + 03: switch, rewind with chromatic aberration, cyan-rimmed Echo, dual-solve the final gate. | Tape-stop SFX into crescendo |
| 2:30–3:00 | Synthesis + outro | Explain Time (rewind + echoes) and Shadow (solid light-matter physics). Credits + NaSCon '26 badge. | Resolving piano chord |

Gameplay segments total 2 min of raw footage. Hook + outro total 1 min of edited footage.

### Submission checklist (must be done by 09:15 AM)
- [ ] Record 1080p60 gameplay (OBS Studio or macOS screen recorder).
- [ ] Edit the 3-minute video (CapCut, Premiere, or DaVinci Resolve) with clean title cards.
- [ ] Upload to Google Drive. Set sharing to "Anyone with the link can view".
- [ ] Build macOS: `unity build UmbraChronos --editor-version <VERSION> --target StandaloneOSX`
- [ ] Build Windows: `unity build UmbraChronos --editor-version <VERSION> --target StandaloneWindows64`
  (`<VERSION>` = value in `ProjectSettings/ProjectVersion.txt`)
- [ ] Public GitHub repo has a clean `README.md`, gameplay GIFs, and all code.
- [ ] Submit Drive link + repo link + build on the NaSCon portal.