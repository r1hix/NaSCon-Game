#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Player;
using UmbraChronos.Time;
using UmbraChronos.Environment;
using UmbraChronos.Core;

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Automation tool to build the Player prefab, Umbral Echo prefab, RewindManager prefab,
    /// interactive puzzle prefabs (PressurePlate, TimedGate, PushableBlock),
    /// and generate the Sandbox_Shaf playground scene with single-click setup.
    /// Upgraded to match the full Master atmospheric aesthetic:
    /// - Multi-layered Hollow Knight / Gris inspired gradient background
    /// - Gothic pillars, cathedral arches, and volumetric chasm mist
    /// - Bioluminescent glow lichens with Point Light 2D breathing
    /// - Flush collision geometry and zero sky bleed
    /// </summary>
    public static class SandboxBuilder
    {
        private const string PlayerPrefabFolder = "Assets/_Project/Prefabs/Player";
        private const string CorePrefabFolder = "Assets/_Project/Prefabs/Core";
        private const string InteractablePrefabFolder = "Assets/_Project/Prefabs/Interactables";
        private const string EnvironmentPrefabFolder = "Assets/_Project/Prefabs/Environment";
        private const string SceneFolderPath = "Assets/_Project/Scenes/Sandboxes";
        private const string SpriteFolderPath = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Setup Shaf Sandbox & Prefabs")]
        public static void BuildSandboxAndPrefabs()
        {
            EnsureDirectoriesExist();

            Sprite squareSprite = GetOrCreateSquareSprite();

            // 1. Core Player & Time Prefabs
            GameObject echoPrefab = CreateEchoPrefab(squareSprite);
            GameObject playerPrefab = CreatePlayerPrefab(squareSprite);
            GameObject rewindManagerPrefab = CreateRewindManagerPrefab(echoPrefab);
            GameObject postProcessingPrefab = CreatePostProcessingPrefab();

            // 2. Interactive Puzzle Prefabs
            GameObject gatePrefab = CreateTimedGatePrefab(squareSprite);
            GameObject platePrefab = CreatePressurePlatePrefab(squareSprite);
            GameObject blockPrefab = CreatePushableBlockPrefab(squareSprite);
            GameObject lethalLightPrefab = CreateLethalLightZonePrefab();
            GameObject sunDialPrefab = CreateSunDialPrefab(squareSprite);
            GameObject monolithPrefab = CreateMonolithPrefab(squareSprite);

            // 3. Assemble Sandbox Scene with Master aesthetics
            BuildSandboxScene(playerPrefab, rewindManagerPrefab, postProcessingPrefab, lethalLightPrefab, sunDialPrefab, gatePrefab, platePrefab, blockPrefab, monolithPrefab, squareSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SandboxBuilder] Sandbox_Shaf.unity and puzzle prefabs generated successfully with unified Master aesthetics!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(PlayerPrefabFolder)) Directory.CreateDirectory(PlayerPrefabFolder);
            if (!Directory.Exists(CorePrefabFolder)) Directory.CreateDirectory(CorePrefabFolder);
            if (!Directory.Exists(InteractablePrefabFolder)) Directory.CreateDirectory(InteractablePrefabFolder);
            if (!Directory.Exists(EnvironmentPrefabFolder)) Directory.CreateDirectory(EnvironmentPrefabFolder);
            if (!Directory.Exists(SceneFolderPath)) Directory.CreateDirectory(SceneFolderPath);
            if (!Directory.Exists(SpriteFolderPath)) Directory.CreateDirectory(SpriteFolderPath);
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            return LevelBuilder.GetOrCreateSquareSprite();
        }

        private static GameObject CreateEchoPrefab(Sprite sprite)
        {
            string path = $"{PlayerPrefabFolder}/UmbralEcho.prefab";

            GameObject echoObj = new GameObject("UmbralEcho");
            echoObj.transform.localScale = Vector3.one;

            var sr = echoObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 5;

            var col = echoObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.65f, 1.48f);
            col.offset = new Vector2(0f, -0.05f);

            echoObj.AddComponent<UmbralEcho>();
            var anim = echoObj.AddComponent<UmbraChronos.Player.PlayerAnimator>();
            anim.ConfigureAsEcho();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(echoObj, path);
            Object.DestroyImmediate(echoObj);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(Sprite sprite)
        {
            string path = $"{PlayerPrefabFolder}/Player.prefab";

            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.localScale = Vector3.one;

            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.65f, 1.48f);
            col.offset = new Vector2(0f, -0.05f);

            playerObj.AddComponent<PlayerController>();
            playerObj.AddComponent<TimeBody>();
            playerObj.AddComponent<UmbraChronos.Player.PlayerAnimator>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerObj, path);
            Object.DestroyImmediate(playerObj);
            return prefab;
        }

        private static GameObject CreateRewindManagerPrefab(GameObject echoPrefab)
        {
            string path = $"{CorePrefabFolder}/RewindManager.prefab";

            GameObject rmObj = new GameObject("RewindManager");
            var rm = rmObj.AddComponent<RewindManager>();

            SerializedObject so = new SerializedObject(rm);
            so.FindProperty("echoPrefab").objectReferenceValue = echoPrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rmObj, path);
            Object.DestroyImmediate(rmObj);
            return prefab;
        }

        private static GameObject CreateTimedGatePrefab(Sprite sprite)
        {
            string path = $"{InteractablePrefabFolder}/TimedGate.prefab";

            GameObject gateObj = new GameObject("TimedGate");
            var gateComp = gateObj.AddComponent<TimedGate>();

            var col = gateObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            // Visual child
            GameObject visualChild = new GameObject("Visual");
            visualChild.transform.SetParent(gateObj.transform, false);
            visualChild.transform.localScale = new Vector3(0.6f, 2.8f, 1f);

            var sr = visualChild.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Stone slab
            sr.sortingOrder = 3;

            SerializedObject so = new SerializedObject(gateComp);
            so.FindProperty("gateRenderer").objectReferenceValue = sr;
            so.FindProperty("openOffset").vector2Value = new Vector2(0f, 3.2f);
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gateObj, path);
            Object.DestroyImmediate(gateObj);
            return prefab;
        }

        private static GameObject CreatePressurePlatePrefab(Sprite sprite)
        {
            string path = $"{InteractablePrefabFolder}/PressurePlate.prefab";

            GameObject plateObj = new GameObject("PressurePlate");
            var plateComp = plateObj.AddComponent<PressurePlate>();

            var col = plateObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 0.4f);
            col.offset = new Vector2(0f, 0.2f);
            col.isTrigger = true;

            // Visual child
            GameObject visualChild = new GameObject("Visual");
            visualChild.transform.SetParent(plateObj.transform, false);
            visualChild.transform.localScale = new Vector3(2.0f, 0.2f, 1f);
            visualChild.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var sr = visualChild.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.85f, 0.45f, 0.1f, 1f); // Amber
            sr.sortingOrder = 1;

            SerializedObject so = new SerializedObject(plateComp);
            so.FindProperty("plateRenderer").objectReferenceValue = sr;
            so.FindProperty("sensorSize").vector2Value = new Vector2(2.0f, 0.5f);
            so.FindProperty("sensorOffset").vector2Value = new Vector2(0f, 0.25f);
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(plateObj, path);
            Object.DestroyImmediate(plateObj);
            return prefab;
        }

        private static GameObject CreatePushableBlockPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/PushableBlock.prefab";

            GameObject blockObj = new GameObject("PushableBlock");
            blockObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var sr = blockObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.25f, 0.25f, 0.35f, 1f); // Slate stone
            sr.sortingOrder = 8;

            var rb = blockObj.AddComponent<Rigidbody2D>();
            rb.mass = 5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = blockObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            blockObj.AddComponent<ShadowCaster2D>();
            blockObj.AddComponent<TimeBody>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockObj, path);
            Object.DestroyImmediate(blockObj);
            return prefab;
        }

        private static GameObject CreatePostProcessingPrefab()
        {
            string path = $"{CorePrefabFolder}/PostProcessingVolume.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject volumeObj = new GameObject("PostProcessingVolume");
            var volume = volumeObj.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(volumeObj, path);
            Object.DestroyImmediate(volumeObj);
            return prefab;
        }

        private static GameObject CreateLethalLightZonePrefab()
        {
            string path = $"{EnvironmentPrefabFolder}/LethalLightZone.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject zoneObj = new GameObject("LethalLightZone");
            zoneObj.AddComponent<LethalLightZone>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zoneObj, path);
            Object.DestroyImmediate(zoneObj);
            return prefab;
        }

        private static GameObject CreateSunDialPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/SunDial.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject sunObj = new GameObject("SunDial");
            var dial = sunObj.AddComponent<SunDialController>();

            var light2D = sunObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Point;
            light2D.pointLightInnerRadius = 0.5f;
            light2D.pointLightOuterRadius = 35f;
            light2D.color = new Color(1.0f, 0.96f, 0.88f, 1f);
            light2D.intensity = 2.0f;
            light2D.shadowsEnabled = true;

            SerializedObject so = new SerializedObject(dial);
            so.FindProperty("sunLight").objectReferenceValue = light2D;
            so.FindProperty("orbitRadius").floatValue = 15f;
            so.FindProperty("currentAngle").floatValue = 140f;
            so.FindProperty("minAngle").floatValue = 15f;
            so.FindProperty("maxAngle").floatValue = 165f;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sunObj, path);
            Object.DestroyImmediate(sunObj);
            return prefab;
        }

        private static GameObject CreateMonolithPrefab(Sprite sprite)
        {
            string path = $"{EnvironmentPrefabFolder}/Monolith.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject monolithObj = new GameObject("Monolith");
            monolithObj.transform.localScale = new Vector3(1.6f, 4.0f, 1f);

            var sr = monolithObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.04f, 0.04f, 0.07f, 1f);
            sr.sortingOrder = 2;

            var box = monolithObj.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;

            monolithObj.AddComponent<EdgeCollider2D>();
            var shadowCollider = monolithObj.AddComponent<DynamicShadowCollider>();
            SerializedObject so = new SerializedObject(shadowCollider);
            so.FindProperty("fallbackGroundY").floatValue = -1.75f;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(monolithObj, path);
            Object.DestroyImmediate(monolithObj);
            return prefab;
        }

        private static void BuildSandboxScene(
            GameObject playerPrefab, 
            GameObject rewindManagerPrefab,
            GameObject postProcessingPrefab,
            GameObject lethalLightPrefab,
            GameObject sunDialPrefab,
            GameObject gatePrefab,
            GameObject platePrefab,
            GameObject blockPrefab,
            GameObject monolithPrefab,
            Sprite squareSprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging
            LevelBuilder.SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.04f, 0.025f, 0.035f), new Vector2(24f, 15f), new Vector2(0f, 1.0f));

            // Load environmental art assets
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolderPath}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolderPath}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolderPath}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolderPath}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LevelBuilder.LoadSprite($"{SpriteFolderPath}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LevelBuilder.LoadSprite($"{SpriteFolderPath}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LevelBuilder.LoadSprite($"{SpriteFolderPath}/Environment/Glow_Lichen_C.png");

            // 2. Multi-Layered Warm Sunset / Dusk Atmospheric Backdrop
            LevelBuilder.SetupAtmosphere(
                squareSprite,
                gradientSprite,
                pillarSprite,
                archSprite,
                mistSprite,
                -16f, 36f,
                new Color(0.95f, 0.62f, 0.48f, 1.0f),  // Warm Dusk Amber Sky Tint
                new Color(0.09f, 0.06f, 0.12f, 0.88f),  // Distant Ruin Silhouette Color
                new Color(0.35f, 0.28f, 0.32f, 1.0f),  // Warm Evening Ambiance
                0.48f,
                new Color(0.98f, 0.85f, 0.80f, 0.75f),  // Warm Sunset Chasm Mist
                new float[] { -12f, -4f, 4f, 12f, 20f, 28f, 34f }, // Pillars
                new float[] { -8f, 8f, 24f },                        // Arches
                new float[] { 0.0f, 18.0f }                          // Mist Chasm Centers
            );

            // 3. Interactive Celestial SunDial (starts at dusk angle)
            GameObject sunInst = (GameObject)PrefabUtility.InstantiatePrefab(sunDialPrefab);
            sunInst.transform.position = new Vector3(1f, 14f, 0f);
            var dialComp = sunInst.GetComponent<SunDialController>();
            if (dialComp != null)
            {
                SerializedObject soDial = new SerializedObject(dialComp);
                soDial.FindProperty("orbitCenter").vector2Value = new Vector2(9.0f, 0f);
                soDial.FindProperty("orbitRadius").floatValue = 15f;
                soDial.FindProperty("currentAngle").floatValue = 140f;
                soDial.FindProperty("followCameraX").boolValue = true;
                soDial.ApplyModifiedProperties();
            }

            // 4. SCREEN 1: Left Cliff, Spawn Shelter, Monolith 1, Mid Plateau, Plate 1, Gate 1
            LevelBuilder.CreatePlatform("Cliff_Left", new Vector3(-8.5f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), squareSprite);
            LevelBuilder.CreatePlatform("Boundary_Wall_Left", new Vector3(-14.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);
            GameObject shelterRoof = LevelBuilder.CreatePlatform("Spawn_Shelter_Roof", new Vector3(-10.0f, 1.5f, 0f), new Vector3(7.0f, 0.6f, 1f), squareSprite);
            LevelBuilder.AddInteriorBackdrop(shelterRoof, -1.75f, 0.5f);

            // Stepping Stone Ledge 1
            LevelBuilder.CreatePlatform("Step_Ledge_1", new Vector3(-5.0f, -1.0f, 0f), new Vector3(2.2f, 0.4f, 1f), squareSprite);

            // Pushable Physics Box in Screen 1
            GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
            blockInst.transform.position = new Vector3(-7.5f, -1.15f, 0f);

            // Monolith 1 at edge of left cliff
            LevelBuilder.CreateMonolithObject(squareSprite, new Vector3(-3.0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Mid Plateau connecting Screen 1 and Screen 2 (x = 4 to 12, surface at y = -1.75)
            GameObject midPlat = LevelBuilder.CreatePlatform("Mid_Plateau", new Vector3(8.0f, -2.5f, 0f), new Vector3(8.0f, 1.5f, 1f), squareSprite);
            LevelBuilder.AddSunlitSheen(midPlat, sunInst.transform);

            GameObject midRoof = LevelBuilder.CreatePlatform("Mid_Shelter_Roof", new Vector3(8.0f, 1.5f, 0f), new Vector3(6.0f, 0.6f, 1f), squareSprite);
            LevelBuilder.AddInteriorBackdrop(midRoof, -1.75f, 0.5f);

            // Puzzle 1: PressurePlate 1 & TimedGate 1
            GameObject gateInst1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst1.name = "TimedGate_1";
            gateInst1.transform.position = new Vector3(8.5f, -0.25f, 0f);

            GameObject plateInst1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plateInst1.name = "PressurePlate_1";
            plateInst1.transform.position = new Vector3(5.5f, -1.65f, 0f);
            var plateComp1 = plateInst1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject soPlate1 = new SerializedObject(plateComp1);
                soPlate1.FindProperty("targetGate").objectReferenceValue = gateInst1.GetComponent<TimedGate>();
                soPlate1.ApplyModifiedProperties();
            }

            // 5. SCREEN 2: Monolith 2, Chasm 2, and Sanctuary Citadel
            LevelBuilder.CreateMonolithObject(squareSprite, new Vector3(14.5f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Sanctuary Citadel
            GameObject sanctPlat = LevelBuilder.CreatePlatform("Sanctuary_Citadel", new Vector3(27.0f, -2.5f, 0f), new Vector3(11.0f, 1.5f, 1f), squareSprite);
            LevelBuilder.AddSunlitSheen(sanctPlat, sunInst.transform);

            GameObject sanctRoof = LevelBuilder.CreatePlatform("Sanctuary_Shelter_Roof", new Vector3(27.0f, 1.5f, 0f), new Vector3(6.0f, 0.6f, 1f), squareSprite);
            LevelBuilder.AddInteriorBackdrop(sanctRoof, -1.75f, 0.5f);

            LevelBuilder.CreatePlatform("Boundary_Wall_Right", new Vector3(32.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);

            // Goal Sanctuary Arch on the Citadel
            LevelBuilder.CreateSanctuaryArch(squareSprite, new Vector3(29.5f, -0.4f, 0f));

            // Continuous Lethal Chasm Receptor Floor
            LevelBuilder.CreatePlatform("Chasm_Floor", new Vector3(10.0f, -7.0f, 0f), new Vector3(52f, 2f, 1f), squareSprite);

            // Bioluminescent Glow Lichens (Warm Radiant Cyan #38BDF8)
            Color duskCyan = new Color(0.24f, 0.76f, 0.98f, 1f);
            LevelBuilder.CreateGlowLichen(lichenC, new Vector3(-11.5f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), duskCyan, 0.55f, 3.2f);
            LevelBuilder.CreateGlowLichen(lichenA, new Vector3(-5.0f, -0.65f, 0f), new Vector3(1.2f, 1.0f, 1f), duskCyan, 0.50f, 3.0f);
            LevelBuilder.CreateGlowLichen(lichenB, new Vector3(-3.2f, -1.65f, 0f), new Vector3(1.1f, 1.2f, 1f), duskCyan, 0.50f, 3.0f);
            LevelBuilder.CreateGlowLichen(lichenA, new Vector3(4.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), duskCyan, 0.50f, 3.0f);
            LevelBuilder.CreateGlowLichen(lichenC, new Vector3(8.0f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), duskCyan, 0.55f, 3.2f);
            LevelBuilder.CreateGlowLichen(lichenA, new Vector3(14.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), duskCyan, 0.50f, 3.0f);
            LevelBuilder.CreateGlowLichen(lichenA, new Vector3(22.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), duskCyan, 0.50f, 3.0f);
            LevelBuilder.CreateGlowLichen(lichenC, new Vector3(27.0f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), duskCyan, 0.55f, 3.2f);
            LevelBuilder.CreateGlowLichen(lichenB, new Vector3(29.5f, -0.65f, 0f), new Vector3(1.0f, 1.2f, 1f), duskCyan, 0.55f, 3.2f);

            // 6. Post Processing & Lethal Light Zone
            PrefabUtility.InstantiatePrefab(postProcessingPrefab);
            GameObject lightZoneInst = (GameObject)PrefabUtility.InstantiatePrefab(lethalLightPrefab);
            var lightZoneComp = lightZoneInst.GetComponent<LethalLightZone>();
            if (lightZoneComp != null)
            {
                SerializedObject soLight = new SerializedObject(lightZoneComp);
                SerializedProperty propSun = soLight.FindProperty("sunTransform");
                if (propSun != null)
                {
                    propSun.objectReferenceValue = sunInst.transform;
                    soLight.ApplyModifiedProperties();
                }
            }

            // 7. Player, Rewind, Audio, HUD
            GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInst.transform.position = new Vector3(-10.5f, -1.0f, 0f);

            PrefabUtility.InstantiatePrefab(rewindManagerPrefab);

            GameObject audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CorePrefabFolder}/AudioManager.prefab");
            if (audioPrefab != null) PrefabUtility.InstantiatePrefab(audioPrefab);

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CorePrefabFolder}/HUD.prefab");
            if (hudPrefab == null) hudPrefab = HUDBuilder.GenerateHUDPrefab();
            if (hudPrefab != null) PrefabUtility.InstantiatePrefab(hudPrefab);

            // Save Scene
            string scenePath = $"{SceneFolderPath}/Sandbox_Shaf.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SandboxBuilder] Saved Sandbox_Shaf to {scenePath}");
        }
    }
}
#endif
