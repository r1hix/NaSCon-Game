# AGENTS.md — Umbra Chronos: Echoes of the Sundial

Rules and reference for AI agents and developers. NaSCon '26 Vibe Coding Game Dev.

**Read order at session start:** `AGENTS.md` (rules) → `README.md` → `SPECSHEET.md` (current status + tasks).
If these files conflict with each other or with the code, tell the user. Do not guess.
Numbers in this file are targets. If the code differs, the code wins, then update the doc.

---

## 1. Mission

- **Event:** NaSCon '26 Vibe Coding Game Dev (FAST-NUCES Islamabad).
- **Deadline:** Sunday 09:30 AM PKT, strict. Internal target: submit by 09:15.
- **Deliverables:**
  1. Trailer video (Google Drive, "Anyone with the link can view"), exactly 3 minutes: 2 min raw gameplay + 1 min edited footage explaining Time + Shadow.
  2. Public GitHub repo with all source and assets.
  3. Standalone build (Windows / macOS / WebGL).
- **Top 5 shortlisting is judged only on the trailer.** If it does not look punchy and polished on video, nobody plays the build. When choosing between two tasks, pick the one that looks better on camera.

---

## 2. Game Overview

- **Themes:** Time (primary), Shadow (secondary). Theme is not genre.
- **World rule:** Celestial light is lethal radiation. Shadows are solid matter. The player cannot cross sunlit chasms but can walk on cast shadows.

| Mechanic | Input | What it does |
| :--- | :--- | :--- |
| Sundial | `Q` / `E` / mouse scroll | Moves the sun along an arc. Monoliths cast shadows that become solid colliders. |
| Rewind | Hold `R` / Left Shift | Pulls player (and sun clock) back along a 5 s circular buffer. Audio pitches down, screen gets chromatic aberration. |
| Umbral Echo | Tap `F` / `C` after rewinding | Leaves a past-self clone that replays your recorded movement. It holds plates and acts as a stepping stone. |

Movement: `A` / `D`, jump `Space`.

---

## 3. Tech Stack

- **Engine:** Unity 6. **Do not hardcode the version.** Read it from `ProjectSettings/ProjectVersion.txt` and use that value wherever `<VERSION>` appears below.
- **Template:** Universal 2D. **Pipeline:** URP with 2D Renderer.
- **Lighting:** 2D Light System (Global Light 2D, Point Light 2D, Shadow Caster 2D).
- **Unity CLI:** `v1.0.0-beta.13`. Check with `unity version` and `unity doctor`.
- **Platforms:** Shaf = macOS (Apple Silicon). Chendline = Windows 11 (x86_64).

---

## 4. Golden Rules (Zero-Conflict Protocol)

### Rule 1: Never merge scenes
Git cannot merge `.unity` files safely. Never edit a scene someone else is editing.
- Feature work happens only in your sandbox:
  - `Assets/_Project/Scenes/Sandboxes/Sandbox_Shaf.unity`
  - `Assets/_Project/Scenes/Sandboxes/Sandbox_Chendline.unity`
- **Master Level Scene:** `Assets/_Project/Scenes/Levels/Level_Master.unity` (single unified contiguous level containing all interconnected puzzle chambers). Only one developer edits the scene file directly at a time (currently Chendline). All reusable puzzle modules are authored as prefabs in `Assets/_Project/Prefabs/` to eliminate scene merge conflicts.

### Rule 2: Prefab-first
Every reusable object is a prefab in `Assets/_Project/Prefabs/`.
To change one: open it in Prefab Mode (or edit in your sandbox) → Overrides → Apply All. Your teammate gets the change on `git pull`, with no scene conflicts.

### Rule 3: Event-driven, decoupled code
Use `TimeEvents` for global signals instead of cross-script references or singletons.

```csharp
namespace UmbraChronos.Core
{
    public static class TimeEvents
    {
        public static System.Action<float> OnSunAngleChanged;   // sun angle in degrees
        public static System.Action<bool>  OnRewindStateChanged; // true = rewinding
        public static System.Action        OnEchoCreated;
        public static System.Action        OnPlayerDied;
        public static System.Action        OnPuzzleSolved;
    }
}
```
Always invoke with `?.Invoke(...)`. If the real `TimeEvents.cs` differs from this snippet, the file wins.

### Rule 4: Git hygiene
- **NEVER push commits or merge PRs to remote (`origin`) without explicit user permission.** You are allowed to create local branches, stage, and commit locally, but you MUST NEVER run `git push` or merge PRs unless the user explicitly gives permission in that exact prompt.
- **Never commit to `main`** unless the user explicitly says so.
- Start from fresh main: `git pull --rebase origin main`, then create a branch:
  `feat/<name>`, `fix/<name>`, `refactor/<module>`, `chore/<task>`.
- **Conventional Commits:** `<type>(<scope>): <imperative description>`
  - Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`.
  - Examples:
    - `feat(player): add coyote time and jump buffering`
    - `fix(shadows): stop edge collider flicker during fast light rotation`
    - `chore(git): update .gitattributes for Unity YAML merge`
- **Meta files:** commit every asset together with its `.meta` file, in the same commit.
- **Never commit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`.

---

## 5. Project Layout

```
Assets/_Project/
├── Audio/            Music/, SFX/
├── Materials/        2D materials, post-processing profiles
├── Prefabs/          Core/, Environment/, Interactables/, Player/
├── Scenes/           Levels/, Sandboxes/
├── Scripts/
│   ├── Core/         TimeEvents.cs, GameManager.cs, AudioManager.cs
│   ├── Environment/  SunDialController.cs, DynamicShadowCollider.cs,
│   │                 LethalLightZone.cs, PressurePlate.cs, TimedGate.cs
│   ├── Player/       PlayerController.cs, PlayerAnimation.cs
│   └── Time/         TimeBody.cs, RewindManager.cs, UmbralEcho.cs, IRewindable.cs
├── Shaders/          Rewind + silhouette shaders
└── Sprites/          Silhouette environment and character sprites
```
Prefab folders: Core (GameManager, TimeManager, Camera), Environment (ShadowPillar, Platform, Monolith, SunDial), Interactables (PressurePlate, TimedGate, GoalDoor, PushableBlock), Player (Player, UmbralEchoGhost).

### Script contracts

| Script | Namespace | Responsibility |
| :--- | :--- | :--- |
| `TimeEvents` | `UmbraChronos.Core` | Static event bus: `OnSunAngleChanged`, `OnRewindStateChanged`, `OnEchoCreated`, `OnPlayerDied`, `OnPuzzleSolved`. |
| `IRewindable` | `UmbraChronos.Time` | Interface: `RecordSnapshot()`, `RewindSnapshot()`, `OnStartRewind()`, `OnStopRewind()`. |
| `PlayerController` | `UmbraChronos.Player` | Snappy 2D platformer on `Rigidbody2D`. Coyote time 0.15 s, jump buffer 0.12 s, variable jump cut, ground raycasts. Implements `IRewindable`. |
| `PlayerAnimation` | `UmbraChronos.Player` | Procedural squash/stretch, run cycle bob, airborne lean, landing impact dust, runic glyph pulse. |
| `TimeBody` | `UmbraChronos.Time` | On rewindable objects. 300-frame (~5 s at 60 Hz) circular buffer of position, velocity, rotation, grounded flag. Driven by `RewindManager`. |
| `RewindManager` | `UmbraChronos.Time` | Handles `R` / Shift. Pitches audio down, drives dynamic chromatic aberration/vignette post-processing, steps all `IRewindable` backward. |
| `UmbralEcho` | `UmbraChronos.Time` | Shadow clone spawned on `F`/`C`. Replays recorded history forward. Has a `Collider2D` so it can hold plates. Ignores collision with player. |
| `SunDialController` | `UmbraChronos.Environment` | Moves the sun light on an orbit from `Q`/`E`/scroll. Invokes `OnSunAngleChanged`. |
| `DynamicShadowCollider` | `UmbraChronos.Environment` | Projects monolith shadows onto receiving ground, updates an `EdgeCollider2D` bridge with cyan rim (`#38BDF8`). |
| `LethalLightZone` | `UmbraChronos.Environment` | Raycasts sun → player with 0.4s grace period. Projects dynamic **Volumetric Godray Hazard Beams** (threshold shaft framing roof shelter lines with floating solar motes; direct player shaft pulsing white-hot on danger, cyan on shadow bridges). |
| `GlowLichen` | `UmbraChronos.Environment` | Safe bioluminescent moss patches emitting soft cyan Point Light 2D (`#38BDF8`, intensity 0.55, radius 3.5m) with sine breathing pulse for ambient navigation. |
| `SunlitGroundSheen` | `UmbraChronos.Environment` | Renders golden sunlit ground sheen (`#FFEAA7`) on exposed floor slabs via zero-alloc raycasting, strictly updating on `OnSunAngleChanged`. |
| `InteriorBackdrop` | `UmbraChronos.Environment` | Obsidian backdrop geometry (`#07070C`, sorting order -12) inside enclosed crypts/chambers to eliminate celestial sky bleed behind interiors. |
| `PressurePlate` | `UmbraChronos.Environment` | Detects Player or Echo, activates runic glow, signals the paired `TimedGate`. Zero-jitter debouncing. |
| `TimedGate` | `UmbraChronos.Environment` | Smooth vertical sliding stone door opened by pressure plates. Anti-softlock design. |
| `ScreenShiftCamera` | `UmbraChronos.Core` | Seamless 2D screen-shift camera controller that transitions rooms when the player crosses chamber boundaries. |
| `RoomDetector` | `UmbraChronos.Core` | Triggers room-boundary transitions for `ScreenShiftCamera` based on player grid location. |
| `HUDController` | `UmbraChronos.UI` | Diegetic HUD: Sundial compass needle, 3-tier non-overlapping rewind ring gauge, solar exposure danger vignette, runic prompt overlay. |
| `MainMenuManager` | `UmbraChronos.UI` | Title screen controller handling Play, Options (Volume slider), Credits, and Quit with seamless scene switching. |
| `MainMenuAudioManager` | `UmbraChronos.UI` | Dynamic title screen audio controller managing ambient drone loop, UI hover crystals, and stone click SFX. |
| `AudioManager` | `UmbraChronos.Core` | Global SFX & BGM coordinator: dynamic rewind pitch-down (0.5x), tape wobble, footsteps, gate grinding, vaporization sizzle. |

---

## 6. Art Direction & Style Guide ("Ink & Solar Flare")

Style: high-contrast monochrome silhouettes against vivid celestial skies (inspired by *Limbo*, *Braid*, and *Gris*).

### Color Palette Tokens

| Token | Hex Code | Visual Application |
| :--- | :--- | :--- |
| **Obsidian Foreground** | `#0D0D15` | Solid terrain, walkable ground, monoliths, architectural ruins, player body silhouette. |
| **Interior Backdrop** | `#07070C` | Enclosed crypt backdrops and interior cave walls (prevents celestial sky bleed). |
| **Searing Sunlight** | `#FFF8E7` &rarr; `#FFD166` | Lethal celestial radiation, sun point light, bloom glare, direct hazard flare. |
| **Warning Threshold** | `#F59E0B` | Volumetric godray threshold framing beam indicating active shelter danger boundary. |
| **Umbral Echo Ghost** | `#1E1B4B` (70% alpha) | Translucent midnight indigo clone silhouette replaying recorded history. |
| **Cyan Rune / Rim** | `#38BDF8` | Echo rim highlight, activated pressure plates, glowing monolith runes, shadow bridge walking rim. |
| **Bioluminescent Lichen** | `#38BDF8` | Safe ambient moss patches emitting soft cyan 2D point light in dark chasms/vaults. |
| **Inactive Mechanism** | `#1E293B` | Dormant pressure plates, unpowered stone glyphs, unlit gate runes. |
| **Sunlit Ground Sheen** | `#FFEAA7` (30% alpha) | Golden surface sheen on sun-exposed floor slabs. |

### Celestial Sky Gradient Cycle

The sky tint shifts dynamically based on SunDial angle $\theta \in [15^\circ, 165^\circ]$:
- **Dawn ($15^\circ - 50^\circ$):** Midnight Indigo (`#1E1B4B`) &rarr; Rose Coral (`#F43F5E`)
- **Noon / Zenith ($50^\circ - 130^\circ$):** Azure Sky (`#0284C7`) &rarr; Radiant White (`#FFFFFF`)
- **Dusk ($130^\circ - 165^\circ$):** Burnt Amber (`#D97706`) &rarr; Deep Crimson Maroon (`#4C0519`)
- **Eclipse (Special / Climax):** Cosmic Void (`#030712`) &rarr; Royal Violet (`#581C87`)

### 2D Sorting Order & Rendering Hierarchy

To maintain clean silhouette separation and prevent visual clipping, strictly adhere to the following `SortingOrder` standards:

| Layer / Element | Sorting Order | Material / Shader | Purpose |
| :--- | :--- | :--- | :--- |
| **Sky Background** | `-20` | Unlit Gradient | Dynamic celestial sky dome. |
| **Ambient Godrays** | `-15` | Additive Unlit | Soft background light shafts through temple skylights. |
| **Interior Backdrops** | `-12` | Unlit Sprite (`#07070C`) | Enclosed chamber walls blocking the sky. |
| **Static Geometry / Platforms** | `0` | Sprite-Lit-Default / Unlit (`#0D0D15`) | Walkable terrain, ruins, monoliths. |
| **Hazard Godray Shafts** | `7` | Additive Unlit Feathered Sprite | Volumetric threshold hazard beams and direct rays. |
| **Player Silhouette** | `10` | Sprite-Lit-Default (`#0D0D15`) | Protagonist character with runic eyes. |
| **Umbral Echo Ghost** | `10` | Transparent Cyan Rim (`#1E1B4B` / `#38BDF8`) | Recorded past-self shadow clone. |
| **Glow Lichens & Runes** | `11` | Additive / Lit Sprite (`#38BDF8`) | Active glyphs and bioluminescent moss. |
| **Floating Particles** | `12` | Additive Particle | Obsidian dust, solar motes, vaporization embers. |
| **Diegetic UI / HUD** | `100` | UI Overlay Canvas / Unlit | Sundial compass, rewind gauge, runic prompts. |

### Lighting & Post-Processing Stack

1. **Light Stack:**
   - 1× `PointLight2D` (Sun): High intensity ($1.5 - 2.2$), casts dynamic 2D shadows via `ShadowCaster2D`.
   - 1× `GlobalLight2D`: Ambient sky fill ($0.15 - 0.25$), color-matched to current celestial quadrant.
   - `GlowLichen` 2D Lights: Local `PointLight2D`, intensity $0.55$, inner radius $0.8\text{ m}$, outer radius $3.5\text{ m}$, color `#38BDF8`.
2. **Post-Processing Volume (URP 2D):**
   - **Bloom:** Threshold `0.85`, Intensity `1.4`, Scatter `0.7` (creates blinding sun glare and radiant runes).
   - **Chromatic Aberration:** Base `0.0`, dynamic ramp to `0.9` during Tape-Reel Rewind.
   - **Vignette:** Base `0.25` (subtle dark corners), ramps to `0.55` red on solar damage, `0.45` cyan on rewind.

---

## 7. Level-Creation Guide (Architectural & Puzzle Standards)

Every chamber and obstacle in `Level_Master.unity` or sandbox scenes MUST comply with the following architectural rules.

### Rule 1: Room Grid Dimensions & Screen-Shift Viewport
- **Grid Cell Dimensions:** Exactly **`26.0` units wide $\times$ `15.0` units high** (matching $16:9$ aspect ratio at `orthographicSize = 7.5`).
- **Standard Room Origin Points:**
  - Wing 1 (Dawn Courtyard): `(0, 0)`
  - Wing 2 (Echo Vault): `(26, 0)`
  - Wing 3 (Clockwork Spire): `(52, 0)`
  - Wing 4 (Eclipse Paradox): `(78, 0)`
- **Zero Blind Jumps:** Every puzzle component—SunDial arc, Monolith, Pressure Plate, Timed Gate, and Exit Archway—**MUST fit entirely within the camera's single screen frame**. Never force the player to make leaps of faith or interact with off-screen objects.
- **Screen-Shift Transitions:** Triggered via `RoomDetector.cs`. When the player crosses the chamber threshold ($X = 13.0, 39.0, 65.0$), the camera smoothly lerps to the adjacent grid center over `0.35` seconds.

### Rule 2: Movement & Jump Metrics
- **Horizontal Run Speed:** Exactly `8.0` units/sec.
- **Max Safe Vertical Jump Reach:** Exactly **`2.8` units** (theoretical peak is $3.2\text{ u}$). Never place a jump ledge higher than $2.8\text{ u}$ without stepping platforms.
- **Max Safe Horizontal Gap Reach:** Exactly **`4.2` units** (theoretical sprint jump is $5.5\text{ u}$).
- **Minimum Headroom Clearance:** Exactly **`2.4` units** vertical opening for all doorways and corridors (player collider is $1.8\text{ u}$ tall).
- **Minimum Platform Thickness:** Exactly **`0.6` units** to prevent high-velocity downward tunneling.

### Rule 3: SunDial & Shadow Physics Geometry
- **Orbit Radius:** The SunDial orbit radius must be between **`14.0` and `16.0` units**, centered horizontally over the room's main lethal chasm.
- **Angle Clamp:** Clamped strictly between **`15.0°` (Dawn)** and **`165.0°` (Dusk)**. Zenith is $90.0^\circ$.
- **Monolith Dimensions:** Height **`5.0` to `8.0` units**, Width **`1.8` to `2.4` units**, base anchored directly into `Layer: Ground`.
- **Ground Receptor Rule:** Every lethal chasm receiving a shadow bridge MUST contain a continuous ground receptor plane. `DynamicShadowCollider` raycasts downward from monolith corners; casting into an infinite void produces invalid colliders.
- **Flush Edge Overlap:** The landing point of the shadow bridge must be flush with or overlap the destination ledge by $\le 0.15\text{ u}$ to ensure seamless running without snagging.

### Rule 4: Lethal Light & Shelter Architecture
- **0.4s Grace Threshold:** Direct solar exposure kills the player after $0.4\text{ s}$ ($3.2\text{ u}$ sprint distance). Never create an unbridged sunlit opening wider than $2.0\text{ u}$.
- **Contemplation Sanctuaries:** Every puzzle chamber MUST provide a sheltered overhang or permanent monolith shadow where the player can safely stand indefinitely while solving the puzzle.
- **Sheltered Entry & Exit Corridors:** Chamber boundaries must be 100% inside sheltered shadow. A screen-shift transition must NEVER spawn or drop the player into lethal light.

### Rule 5: Tape-Reel Rewind & Umbral Echo Budget
- **Circular Buffer Capacity:** Exactly **`5.0` seconds** ($300$ frames at $60\text{ Hz}$).
- **The 3.5s Execution Rule:** The sequence of actions required from the Echo (sprinting, climbing, stepping onto a plate) must take **$\le 3.5$ seconds**, leaving at least **`1.5` seconds of rewind buffer** to pull the player back to safety before deploying the clone.
- **Plate Hold Duration:** The player must record holding the plate for at least **`1.5` to `2.0` seconds** so the clone keeps the gate open long enough for the player to pass through.
- **Anti-Softlock Architecture:** All timed gates slide vertically upward into ceilings. If a gate closes, the player must either be able to rewind backward out of the gate or have access to an internal release mechanism.

### Rule 6: Ambient Illumination & Hazard Readability
- **Glow Lichen Placement:** Place `GlowLichen` prefabs near deep drops, vertical staircases, and beneath dark overhangs so the player can discern terrain contours without rotating the sun.
- **Volumetric Hazard Thresholds:** Keep roof shelter lips clean and unblocked so `LethalLightZone` can project the amber threshold godray shaft directly to the floor.

---

## 8. Code Rules for Agents

1. **2D only.** Use `Vector2`, `Rigidbody2D`, `Collider2D`, `Physics2D`. Never mix in 3D physics.
2. **Inspector fields:** `[SerializeField] private`, not `public`.
3. **Null-safe events:** `TimeEvents.OnX?.Invoke(...)`.
4. **Game feel matters.** Crisp movement, high fall gravity. Sluggish controls ruin the trailer.
5. **Namespace gotcha:** a namespace called `UmbraChronos.Time` exists. Inside any `UmbraChronos.*` namespace, a bare `Time.deltaTime` can fail (CS0118). Write `UnityEngine.Time.deltaTime` or add `using Time = UnityEngine.Time;`.
6. **Stay in scope.** Edit only files for the task you were given. Do not touch the other developer's scene.

---

## 9. Unity CLI Reference

Replace `<VERSION>` with the value from `ProjectSettings/ProjectVersion.txt`.

```bash
unity doctor                                              # check environment
unity version                                             # CLI version
unity open [project-path] --editor-version <VERSION>      # open in editor
unity recompile                                           # recompile scripts
unity test [project-path] --editor-version <VERSION> --mode EditMode \
     --report-format junit --output ./test-results.xml    # run tests (or --mode PlayMode)
unity build [project-path] --editor-version <VERSION> --target StandaloneOSX
unity build [project-path] --editor-version <VERSION> --target StandaloneWindows64
```

Rules:
1. Never pass `-batchmode` or `-quit` to `unity run` / `unity build`. The CLI handles it.
2. For headless runs use `--non-interactive --yes`.
3. Prefer `--format json` and check `success: true` in the output, not just the exit code.
4. If a command fails with a schema error, run `unity self-update`.

---

## 10. Ownership (summary)

- **Shaf (Mac):** player, rewind, echo, plates/gates, lethal light, rewind VFX, HUD, audio, camera polish.
- **Chendline (Win):** sundial, shadow colliders, master level layout (`Level_Master.unity`), Windows build.

Detailed tasks, times, and status are in `SPECSHEET.md`.