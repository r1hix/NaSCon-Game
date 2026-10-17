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
- Level scenes (`Level_01`, `Level_02`, `Level_03`) have one author at a time. Currently Chendline.

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
| `TimeEvents` | `UmbraChronos.Core` | Static event bus (see Rule 3). |
| `IRewindable` | `UmbraChronos.Time` | Interface: `RecordSnapshot()`, `RewindSnapshot()`, `OnStartRewind()`, `OnStopRewind()`. |
| `PlayerController` | `UmbraChronos.Player` | Snappy 2D platformer on `Rigidbody2D`. Coyote time 0.15 s, jump buffer 0.12 s, heavier gravity on fall, ground raycast. Implements `IRewindable`. |
| `TimeBody` | `UmbraChronos.Time` | On rewindable objects. 300-frame (~5 s at 60 Hz) circular buffer of position, velocity, rotation, grounded flag. Driven by `RewindManager`. |
| `RewindManager` | `UmbraChronos.Time` | Handles `R` / Shift. Pitches audio down, ramps rewind VFX, steps all `IRewindable` backward. |
| `UmbralEcho` | `UmbraChronos.Time` | Shadow clone. Replays recorded history forward. Has a `Collider2D` so it can hold plates. Ignores collision with the player. |
| `SunDialController` | `UmbraChronos.Environment` | Moves the sun light on an orbit from `Q`/`E`/scroll. Invokes `OnSunAngleChanged`. |
| `DynamicShadowCollider` | `UmbraChronos.Environment` | Projects a monolith's shadow onto the ground and updates an `EdgeCollider2D` so the shadow is solid. |
| `LethalLightZone` | `UmbraChronos.Environment` | Raycasts sun → player. Direct exposure past the grace period invokes `OnPlayerDied`. |
| `PressurePlate` | `UmbraChronos.Environment` | Detects Player or Echo, signals the paired `TimedGate`. Must not jitter. |
| `TimedGate` | `UmbraChronos.Environment` | Smooth vertical sliding stone door, opened by plates. |

---

## 6. Art Direction ("Ink & Solar Flare")

Style: high-contrast monochrome silhouettes against vivid skies (think Limbo, Braid, Gris).

| Element | Color / setting |
| :--- | :--- |
| Solid matter (player, ruins, monoliths, shadow floors) | `#0D0D15` |
| Sunlight (lethal) | `#FFF8E7` → `#FFD166` |
| Umbral Echo | `#1E1B4B`, translucent, cyan rim `#38BDF8` at 70% opacity |
| Lethal light zones | White-hot glare + URP Bloom (threshold 0.85, intensity 1.4) |
| Sky: Dawn | `#1E1B4B` → `#F43F5E` |
| Sky: Noon | `#0284C7` → `#FFFFFF` |
| Sky: Dusk | `#D97706` → `#4C0519` |
| Sky: Eclipse | `#030712` → `#581C87` |

- Use Sprite-Lit-Default or unlit silhouette shaders for level geometry.
- Light stack: 1× Point Light 2D (sun), 1× Global Light 2D (sky tint), `ShadowCaster2D` on every monolith.
- Rewind filter: global Volume with Chromatic Aberration (target 0.9), Vignette 0.45, scanlines, pitch-shifted audio.

---

## 7. Code Rules for Agents

1. **2D only.** Use `Vector2`, `Rigidbody2D`, `Collider2D`, `Physics2D`. Never mix in 3D physics.
2. **Inspector fields:** `[SerializeField] private`, not `public`.
3. **Null-safe events:** `TimeEvents.OnX?.Invoke(...)`.
4. **Game feel matters.** Crisp movement, high fall gravity. Sluggish controls ruin the trailer.
5. **Namespace gotcha:** a namespace called `UmbraChronos.Time` exists. Inside any `UmbraChronos.*` namespace, a bare `Time.deltaTime` can fail (CS0118). Write `UnityEngine.Time.deltaTime` or add `using Time = UnityEngine.Time;`.
6. **Stay in scope.** Edit only files for the task you were given. Do not touch the other developer's scene.

---

## 8. Unity CLI Reference

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

## 9. Ownership (summary)

- **Shaf (Mac):** player, rewind, echo, plates/gates, lethal light, rewind VFX, HUD, audio.
- **Chendline (Win):** sundial, shadow colliders, levels 01–03, Windows build.

Detailed tasks, times, and status are in `SPECSHEET.md`.