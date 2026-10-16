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
* **Rendering Pipeline:** Universal Render Pipeline (URP) with **2D Renderer**.
* **Lighting:** 2D Light System (Global Light 2D, Freeform Light 2D, Shadow Caster 2D).
* **Cross-Platform Target:**
  * Developer 1 (Shaf): **macOS (Apple Silicon arm64)**.
  * Developer 2 (Partner): **Windows 11 PC (x86_64)**.
  * Target Builds: Windows `.exe` / macOS `.app` / WebGL.

---

## 4. Golden Rules for Developers & Agents (Zero-Conflict Protocol)

### Rule 1: The "Never Merge Scenes" Invariant
* **Git cannot merge Unity `.unity` scene files without corruption.**
* **Never edit the same scene.**
* **All feature work happens in personal sandbox scenes:**
  * `Assets/_Project/Scenes/Sandboxes/Sandbox_Shaf.unity`
  * `Assets/_Project/Scenes/Sandboxes/Sandbox_Partner.unity`
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
│   │   └── Sandboxes/        # Sandbox_Shaf, Sandbox_Partner
│   ├── Scripts/
│   │   ├── Core/             # TimeEvents.cs, GameManager.cs
│   │   ├── Environment/      # SunDial.cs, DynamicShadowCollider.cs, PressurePlate.cs
│   │   ├── Player/           # PlayerController.cs, PlayerAnimation.cs
│   │   └── Time/             # TimeBody.cs, RewindManager.cs, UmbralEcho.cs
│   ├── Shaders/              # Rewind post-processing & silhouette shaders
│   └── Sprites/              # 2D Silhouette environment and character sprites
```

---

## 6. Task Division & Ownership

| Developer | Core Responsibilities | Key Deliverables |
| :--- | :--- | :--- |
| **Shaf (Mac)** | **Player Experience, Time Mechanics & Polish** | • `PlayerController.cs` (Snappy 2D movement, jump buffering, coyote time)<br>• `TimeBody.cs` & `RewindManager.cs` (Circular buffer state recorder)<br>• `UmbralEcho.cs` (Ghost recording & playback)<br>• Post-processing / Rewind screen VFX (Chromatic aberration, scanlines)<br>• HUD UI (Time dial indicator, Rewind energy meter) |
| **Partner (Win)** | **Shadow Generation, Level Design & Puzzle Logic** | • `SunDialController.cs` (Rotates 2D Light smoothly via input)<br>• `DynamicShadowCollider.cs` (Generates walkable edge/polygon colliders from shadow casters)<br>• Puzzle prefabs: `PressurePlate.cs`, `TimedGate.cs`, `LethalLightZone.cs`<br>• Level blockouts (`Level_01`, `Level_02`, `Level_03`)<br>• Audio integration (SFX for ticking, rewinds, footfalls) |

---

## 7. Instructions for Agents Making Code Edits

1. **Keep it 2D:** Use `Vector2`, `Rigidbody2D`, `Collider2D`, `Physics2D`. Do NOT mix 3D physics with 2D physics.
2. **Explicit Fields:** Use `[SerializeField] private ...` instead of `public` variables for inspector tuning.
3. **Null-Safety:** Always check event subscriptions with null-conditional invocations: `TimeEvents.OnSunAngleChanged?.Invoke(angle);`.
4. **Game Feel (Juice):** When writing player movement, make it feel crisp (high gravity scale on descent, coyote time 0.15s, jump buffer 0.1s). Sluggish movement ruins gameplay trailers!
