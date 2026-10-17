# AGENTS.md — Umbra Chronos: Echoes of the Sundial
**Project Guide for AI Agents & Developers | NaSCon '26 Vibe Coding Game Dev**

---

## 1. Hackathon Constraints & Mission Brief

* **Event:** NaSCon '26 — *Vibe Coding Game Dev Competition* (FAST-NUCES Islamabad).
* **Final Submission Deadline:** **Sunday 09:30 AM PKT** (Strict cutoff).
* **Primary Submission Deliverables:**
  1. **Trailer Video Link (Google Drive):** Exactly 3 minutes total.
     * **2 minutes:** Pure, unedited screen-recorded gameplay showcasing mechanics and level flow.
     * **1 minute:** Stylistic editing/cinematic explaining how **Time** and **Shadow** are integrated.
     * *CRITICAL RULE:* Initial evaluation and the **Top 5 finalist shortlisting is judged SOLELY on the trailer**. If the gameplay doesn't look polished and punchy on video, judges will never play the build.
  2. **GitHub Public Repository:** Clean repository containing all source code and assets.
  3. **Build:** Standalone executable (Windows / Mac) or WebGL.

---

## 2. Game Overview & Theme Synergy

* **Title:** *Umbra Chronos: Echoes of the Sundial*
* **Core Themes:** **Time** (Primary) & **Shadow** (Secondary). *(Remember: Theme ≠ Genre).*
* **Core Rule of the World:**
  * **Celestial light is lethal radiation; Shadows are physical, solid matter.**
  * You cannot jump across chasms of burning sunlight, but you can walk, climb, and slide across the solid geometric shadows cast by ancient clockwork ruins.

### The Mechanical Triad:
1. **The Celestial Sundial (`Q` / `E` / Mouse Scroll):**
   * Rotate the celestial light source across an arc in the sky (dawn &rarr; noon &rarr; dusk &rarr; eclipse).
   * Monoliths, gears, and pillars dynamically cast elongating and contracting 2D shadows that generate real-time physical colliders.
2. **The Tape-Reel Rewind (`R` / Left Shift):**
   * Holding rewind pulls physics, player trajectory, and the celestial clock backward along a 5-second circular buffer.
   * Audio pitches down into a reverse tape-stop; screen distorts with subtle chromatic aberration and scanlines.
3. **The Umbral Echo (`E` / Space + Rewind):**
   * Snapping back in time can leave your past self behind as an **Umbral Echo** (a living silhouette shadow clone).
   * The Echo replays your past actions: holding pressure plates, pulling timed levers, or serving as a physical stepping-stone for the present player.

---

## 3. Tech Stack & Environment

* **Engine:** Unity 6 (`6000.6.3f1` / `6000.6.x LTS`).
* **Unity CLI:** Unity CLI `v1.0.0-beta.13` (Manage with `unity self-update` / `unity doctor`).
* **Template:** Universal 2D (`com.unity.template.universal-2d`).
* **Rendering Pipeline:** Universal Render Pipeline (URP) with **2D Renderer**.
* **Lighting:** 2D Light System (Global Light 2D, Freeform Light 2D, Shadow Caster 2D).
* **Cross-Platform Target:**
  * Developer 1 (Shaf): **macOS (Apple Silicon arm64)**.
  * Developer 2 (Chendline): **Windows 11 PC (x86_64)**.
  * Target Builds: Windows `.exe` / macOS `.app` / WebGL.

---

## 4. Golden Rules for Developers & Agents (Zero-Conflict Protocol)

### Rule 1: The "Never Merge Scenes" Invariant
* **Git cannot merge Unity `.unity` scene files without corruption.**
* **Never edit the same scene.**
* **All feature work happens in personal sandbox scenes:**
  * `Assets/_Project/Scenes/Sandboxes/Sandbox_Shaf.unity`
  * `Assets/_Project/Scenes/Sandboxes/Sandbox_Chendline.unity`
* Formal game levels are authored by ONE person at a time (`Level_01.unity`, `Level_02.unity`, etc.).

### Rule 2: Prefab-First Architecture
* **Every reusable object must be a Prefab** inside `Assets/_Project/Prefabs/`.
* When updating player mechanics, lighting, or puzzle components:
  1. Open the Prefab in Prefab Mode or adjust in your sandbox.
  2. Hit **Overrides &rarr; Apply All**.
  3. When your teammate pulls from Git, their sandbox automatically updates without scene conflicts!

### Rule 3: Event-Driven Decoupled Code
* Avoid direct cross-script coupling or messy singletons where possible.
* Use `TimeEvents.cs` for global gameplay signals:
  ```csharp
  public static class TimeEvents
  {
      public static System.Action<float> OnSunAngleChanged;   // angle in degrees (0 - 360)
      public static System.Action<bool> OnRewindStateChanged; // true = active, false = normal
      public static System.Action OnEchoCreated;
      public static System.Action OnPlayerDied;
  }
  ```

### Rule 4: Clean Git Hygiene & AI Commit Protocol
* **Branch-First Rule for AI Agents:**
  * When an AI agent is tasked with making edits or committing code, it must **never commit directly to `main`** unless explicitly instructed by the user.
  * Always create and work on dedicated feature/bugfix branches:
    * `feat/<feature-name>` (e.g. `feat/player-controller`, `feat/time-rewind-buffer`)
    * `fix/<bug-name>` (e.g. `fix/collider-drift`, `fix/shadow-rotation`)
    * `refactor/<module>`
    * `chore/<task>`
  * Before branching, ensure your local main is up to date: `git pull --rebase origin main`.
* **Conventional Commits Standard:**
  * All commit messages written by developers or AI agents **must follow the Conventional Commits specification**:
    ```
    <type>(<scope>): <imperative present-tense description>
    ```
  * **Allowed Types:**
    * `feat`: A new feature or gameplay mechanic
    * `fix`: A bug fix or patch
    * `docs`: Documentation updates (`README.md`, `AGENTS.md`, design docs)
    * `style`: Formatting, whitespace, or code styling adjustments
    * `refactor`: Code restructures that neither fix a bug nor add a feature
    * `perf`: Performance optimizations (e.g., GC allocation reduction)
    * `test`: PlayMode or EditMode unit test scripts
    * `chore`: Build scripts, project configs, `.gitignore`, package dependencies
  * **Examples:**
    * `feat(player): implement coyote time and jump buffering in PlayerController`
    * `feat(time): add circular buffer history recording for rewind mechanic`
    * `fix(shadows): prevent edge collider vertex flickering during rapid light rotation`
    * `chore(git): update .gitattributes for Unity YAML merge tool`
* **Atomic Meta-File Pairing:**
  * Never commit a Unity asset without its corresponding `.meta` file. Whenever you create, rename, or move an asset, commit both the asset AND its `.meta` file in the same commit.
  * Never commit cache folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`).

---

## 5. Architecture & File Hierarchy

```
Assets/
├── _Project/
│   ├── Audio/
│   │   ├── Music/            # Ambient piano / clockwork drone
│   │   └── SFX/              # Ticks, tape-rewind, jumps, stone impacts
│   ├── Materials/            # 2D Lit/Unlit materials & post-processing profiles
│   ├── Prefabs/
│   │   ├── Core/             # GameManager, TimeManager, Camera
│   │   ├── Environment/      # ShadowPillar, Platform, Monolith, SunDial
│   │   ├── Interactables/    # PressurePlate, TimedGate, GoalDoor
│   │   └── Player/           # Player, UmbralEchoGhost
│   ├── Scenes/
│   │   ├── Levels/           # Level_01, Level_02, Level_03
│   │   └── Sandboxes/        # Sandbox_Shaf, Sandbox_Chendline
│   ├── Scripts/
│   │   ├── Core/             # TimeEvents.cs, GameManager.cs
│   │   ├── Environment/      # SunDial.cs, DynamicShadowCollider.cs, PressurePlate.cs, LethalLightZone.cs
│   │   ├── Player/           # PlayerController.cs, PlayerAnimation.cs
│   │   └── Time/             # TimeBody.cs, RewindManager.cs, UmbralEcho.cs, IRewindable.cs
│   ├── Shaders/              # Rewind post-processing & silhouette shaders
│   └── Sprites/              # 2D Silhouette environment and character sprites
```

### 5.1 Art Style & Visual Direction ("The Ink & Solar Flare" Aesthetic)

* **Visual Identity:** High-contrast monochromatic silhouettes grounded against vibrant, shifting celestial skies (Inspirations: *Limbo*, *Braid*, *Gris*).
* **Color Palette & Exact Tokens:**
  * **Solid Obsidian Matter:** `#0D0D15` (Player, stone ruins, monoliths, solid shadow floors).
  * **Searing Celestial Sunlight:** `#FFF8E7` &rarr; `#FFD166` (Blinding light source; lethal radiation).
  * **Umbral Echo (Past Clone):** `#1E1B4B` (Translucent ink silhouette with glowing cyan rim `#38BDF8` at 70% opacity).
  * **Lethal Light Zones:** Searing white-hot glare enhanced with URP Bloom (Threshold: `0.85`, Intensity: `1.4`).
  * **Sky Gradients across the Sun Arc:**
    * *Dawn (0° - 90°):* Deep Midnight Indigo (`#1E1B4B`) &rarr; Rose Coral (`#F43F5E`)
    * *Noon (90° - 180°):* Azure Sky (`#0284C7`) &rarr; White Hot Solar Flare (`#FFFFFF`)
    * *Dusk (180° - 270°):* Burnt Amber (`#D97706`) &rarr; Deep Wine Maroon (`#4C0519`)
    * *Eclipse (270° - 360°):* Obsidian Void (`#030712`) &rarr; Cosmic Violet (`#581C87`)
* **Lighting & Shaders:**
  * Use **Sprite-Lit-Default** or unlit pure silhouette shaders for all level geometry.
  * URP 2D Light Stack: 1x Point Light 2D for the orbital sun, 1x Global Light 2D for ambient sky tint, and `ShadowCaster2D` components attached to all monoliths.
  * **Rewind Screen Filter:** Post-processing Volume with dynamic Chromatic Aberration (`0.8`), subtle Vignette (`0.45`), and pitch-shifted audio to instantly sell the tape-rewind sensation.

### 5.2 Core Script Architecture & Responsibility Breakdown

| Script | Namespace & Location | Primary Responsibilities & Contracts |
| :--- | :--- | :--- |
| **`TimeEvents.cs`** | `UmbraChronos.Core`<br>`Scripts/Core/` | Global static event bus. Decouples player, sundial, rewind manager, and interactables. Exposes `OnSunAngleChanged`, `OnRewindStateChanged`, `OnEchoCreated`, `OnPlayerDied`, `OnPuzzleSolved`. |
| **`IRewindable.cs`** | `UmbraChronos.Time`<br>`Scripts/Time/` | Interface implemented by any object that rewinds. Defines `RecordSnapshot()`, `RewindSnapshot()`, `OnStartRewind()`, `OnStopRewind()`. |
| **`PlayerController.cs`** | `UmbraChronos.Player`<br>`Scripts/Player/` | Snappy 2D platformer movement (`Rigidbody2D`). Features coyote time (0.15s), jump buffering (0.1s), gravity scaling on fall, ground detection raycasts. Implements `IRewindable`. |
| **`TimeBody.cs`** | `UmbraChronos.Time`<br>`Scripts/Time/` | Component attached to rewindable entities. Stores a 300-frame (~5 sec at 60Hz) circular buffer of `(Vector2 pos, Vector2 vel, float rot, bool isGrounded)`. Driven by `RewindManager`. |
| **`RewindManager.cs`** | `UmbraChronos.Time`<br>`Scripts/Time/` | Global controller for the rewind mechanic. Listens for `R` / `Shift` input, slows audio pitch, ramps chromatic aberration VFX, and commands all `IRewindable` components to step backward. |
| **`UmbralEcho.cs`** | `UmbraChronos.Time`<br>`Scripts/Time/` | Instantiated shadow clone. Receives recorded snapshot history buffer and replays it in forward time. Possesses physical `Collider2D` to hold pressure plates and trigger switches. |
| **`SunDialController.cs`** | `UmbraChronos.Environment`<br>`Scripts/Environment/` | Moves the celestial 2D light source across an orbital path using `Q`/`E` or mouse scroll. Invokes `TimeEvents.OnSunAngleChanged(float angle)`. |
| **`DynamicShadowCollider.cs`** | `UmbraChronos.Environment`<br>`Scripts/Environment/` | Reads the 2D shadow caster geometry projected by sunlight and updates an `EdgeCollider2D` or `PolygonCollider2D` in real time so cast shadows act as solid ground. |
| **`PressurePlate.cs`** | `UmbraChronos.Environment`<br>`Scripts/Environment/` | Detects physical contact from `Player` OR `UmbralEcho`. Broadcasts activation signals to paired `TimedGate` or puzzle doors. |
| **`LethalLightZone.cs`** | `UmbraChronos.Environment`<br>`Scripts/Environment/` | Performs raycasts from the orbital sun to the player. If the player steps outside physical shadows into direct sunlight, invokes `TimeEvents.OnPlayerDied`. |

---

## 6. Task Division & Ownership

> 📖 **Master Step-by-Step Schedule:** Refer to [`SPECSHEET.md`](file:///Users/shaf/Desktop/projects/NaSCon%20%2726/GameDev/SPECSHEET.md) for the hour-by-hour parallel development checklist, component contracts, level designs, and submission countdown.

| Developer | Core Responsibilities | Key Deliverables |
| :--- | :--- | :--- |
| **Shaf (Mac)** | **Player Experience, Time Mechanics & Polish** | • `PlayerController.cs` (Snappy 2D movement, jump buffering, coyote time)<br>• `TimeBody.cs` & `RewindManager.cs` (Circular buffer state recorder)<br>• `UmbralEcho.cs` (Ghost recording & playback)<br>• `PressurePlate.cs` & `TimedGate.cs` (Zero-jitter puzzle sensors)<br>• `LethalLightZone.cs` (Deadly sunlight raycast zones)<br>• Post-processing / Rewind screen VFX (Chromatic aberration, scanlines)<br>• HUD UI (Time dial indicator, Rewind energy meter)<br>• Audio & SFX integration |
| **Chendline (Win)** | **Shadow Generation, Sun Control & Level Design** | • `SunDialController.cs` (Rotates 2D Light smoothly via input, invokes `TimeEvents.OnSunAngleChanged`)<br>• `DynamicShadowCollider.cs` (Generates walkable edge colliders from cast shadows)<br>• Level blockouts & puzzle assembly (`Level_01`, `Level_02`, `Level_03`)<br>• Testing & balancing in `Sandbox_Chendline.unity`<br>• Standalone Windows build generation |

---

## 7. Instructions for Agents Making Code Edits

1. **Keep it 2D:** Use `Vector2`, `Rigidbody2D`, `Collider2D`, `Physics2D`. Do NOT mix 3D physics with 2D physics.
2. **Explicit Fields:** Use `[SerializeField] private ...` instead of `public` variables for inspector tuning.
3. **Null-Safety:** Always check event subscriptions with null-conditional invocations: `TimeEvents.OnSunAngleChanged?.Invoke(angle);`.
4. **Game Feel (Juice):** When writing player movement, make it feel crisp (high gravity scale on descent, coyote time 0.15s, jump buffer 0.1s). Sluggish movement ruins gameplay trailers!

---

## 8. Unity CLI Tooling Standards & Automation Reference

The project uses the modern **Unity CLI (`v1.0.0-beta.13`)** for headless automation, project initialization, testing, and builds.

### Key CLI Operations:
* **Project Scaffolding (Universal 2D):**
  ```bash
  unity projects new UmbraChronos --editor-version 6000.6.3f1 --template com.unity.template.universal-2d --path .
  ```
* **Opening Project in Editor:**
  ```bash
  unity open [project-path] --editor-version 6000.6.3f1
  ```
* **Recompiling Active Scripts:**
  ```bash
  unity recompile
  ```
* **Running Automated Tests (PlayMode / EditMode):**
  ```bash
  unity test [project-path] --editor-version 6000.6.3f1 --mode EditMode --report-format junit --output ./test-results.xml
  ```
* **Building Executables:**
  ```bash
  # macOS build
  unity build [project-path] --editor-version 6000.6.3f1 --target StandaloneOSX
  # Windows build
  unity build [project-path] --editor-version 6000.6.3f1 --target StandaloneWindows64
  ```
* **Environment Diagnostics:**
  ```bash
  unity doctor
  ```

### CLI Automation Invariants for AI Agents:
1. **Never pass `-batchmode` or `-quit`** to `unity run` or `unity build`. The CLI automatically manages batch execution and forwarding.
2. **Suppress interactive prompts** in headless runs using `--non-interactive --yes`.
3. **Machine parsing:** Prefer `--format json` for predictable output envelopes. Check `success: true` in the output envelope rather than relying solely on process return codes.
4. **Stay up to date:** Check CLI status with `unity version` and run `unity self-update` if a major release breaks schema.
